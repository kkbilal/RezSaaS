using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class BusinessAppointmentQueryService
{
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessAppointmentQueryService(
        BookingDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<BusinessAppointmentListItemView>> GetAsync(
        BusinessAppointmentQuery requestQuery,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return [];
        }

        IQueryable<Appointment> query = dbContext.Appointments
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .Where(entity => entity.StartUtc < requestQuery.ToUtc
                && entity.EndUtc > requestQuery.FromUtc);

        if (requestQuery.BranchId is not null)
        {
            query = query.Where(entity => entity.BranchId == requestQuery.BranchId);
        }

        if (AppointmentStatusFilter.TryParse(requestQuery.Status, out AppointmentStatus status))
        {
            query = query.Where(entity => entity.Status == status);
        }

        List<Appointment> appointments = await query
            .OrderBy(entity => entity.StartUtc)
            .Take(Math.Clamp(requestQuery.Take, 1, 250))
            .ToListAsync(cancellationToken);

        return appointments
            .Select(ToListItemView)
            .ToArray();
    }

    public async Task<BusinessAppointmentListItemView?> GetByIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null || appointmentId == Guid.Empty)
        {
            return null;
        }

        Appointment? appointment = await dbContext.Appointments
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .SingleOrDefaultAsync(entity => entity.Id == appointmentId, cancellationToken);

        return appointment is null ? null : ToListItemView(appointment);
    }

    public async Task<BusinessAppointmentAuthorizationContext?> GetAuthorizationContextAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null || appointmentId == Guid.Empty)
        {
            return null;
        }

        return await dbContext.Appointments
            .AsNoTracking()
            .Where(entity => entity.Id == appointmentId)
            .Select(entity => new BusinessAppointmentAuthorizationContext(
                entity.Id,
                entity.BranchId,
                entity.CustomerUserAccountId,
                entity.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static BusinessAppointmentListItemView ToListItemView(Appointment appointment)
    {
        return new BusinessAppointmentListItemView(
            appointment.Id,
            appointment.AppointmentRequestId,
            appointment.CustomerUserAccountId,
            appointment.BranchId,
            appointment.StaffMemberId,
            appointment.ResourceId,
            appointment.StartUtc,
            appointment.EndUtc,
            appointment.Status.ToString(),
            appointment.BusinessNote,
            appointment.CancelledAtUtc,
            appointment.CancellationReason,
            appointment.CompletedAtUtc,
            appointment.CompletionNote,
            appointment.NoShowAtUtc,
            appointment.NoShowReason,
            appointment.RebookedFromAppointmentId,
            appointment.RebookedToAppointmentId,
            appointment.RebookedAtUtc,
            appointment.RebookReason,
            appointment.Lines
                .Select(line => new BusinessAppointmentLineView(
                    line.ServiceVariantId,
                    line.ServiceNameSnapshot,
                    line.DurationMinutes,
                    line.PriceAmount,
                    line.CurrencyCode))
                .ToArray());
    }
}
