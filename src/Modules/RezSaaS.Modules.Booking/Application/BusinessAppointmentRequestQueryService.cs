using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class BusinessAppointmentRequestQueryService
{
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessAppointmentRequestQueryService(
        BookingDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<BusinessAppointmentRequestListItemView>> GetPendingAsync(
        Guid? branchId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return [];
        }

        IQueryable<AppointmentRequest> query = dbContext.AppointmentRequests
            .AsNoTracking()
            .Include(entity => entity.Lines)
            .Where(entity => entity.Status == AppointmentRequestStatus.PendingApproval);

        if (branchId is not null)
        {
            query = query.Where(entity => entity.BranchId == branchId);
        }

        List<AppointmentRequest> requests = await query
            .OrderBy(entity => entity.RequestedStartUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);

        return requests
            .Select(entity => new BusinessAppointmentRequestListItemView(
                entity.Id,
                entity.CustomerUserAccountId,
                entity.BranchId,
                entity.StaffMemberId,
                entity.ResourceId,
                entity.RequestedStartUtc,
                entity.RequestedEndUtc,
                entity.ExpiresAtUtc,
                entity.Status.ToString(),
                entity.Lines
                    .Select(line => new BusinessAppointmentRequestLineView(
                        line.ServiceVariantId,
                        line.ServiceNameSnapshot,
                        line.DurationMinutes,
                        line.PriceAmount,
                        line.CurrencyCode))
                    .ToArray()))
            .ToArray();
    }

    public async Task<BusinessAppointmentRequestAuthorizationContext?> GetAuthorizationContextAsync(
        Guid appointmentRequestId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return null;
        }

        return await dbContext.AppointmentRequests
            .AsNoTracking()
            .Where(entity => entity.Id == appointmentRequestId)
            .Select(entity => new BusinessAppointmentRequestAuthorizationContext(
                entity.Id,
                entity.BranchId,
                entity.Status.ToString()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
