using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class ConfirmedAppointmentQueryService
{
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public ConfirmedAppointmentQueryService(
        BookingDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<ConfirmedAppointmentBusyTimeView>> GetBusyTimesAsync(
        Guid branchId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return [];
        }

        return await dbContext.Appointments
            .AsNoTracking()
            .Where(entity => entity.BranchId == branchId
                && entity.Status == AppointmentStatus.Confirmed
                && entity.StartUtc < toUtc
                && entity.EndUtc > fromUtc)
            .OrderBy(entity => entity.StartUtc)
            .Select(entity => new ConfirmedAppointmentBusyTimeView(
                entity.StaffMemberId,
                entity.ResourceId,
                entity.StartUtc,
                entity.EndUtc))
            .ToListAsync(cancellationToken);
    }
}
