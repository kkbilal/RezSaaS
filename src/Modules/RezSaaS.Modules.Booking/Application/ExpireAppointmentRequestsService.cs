using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;

namespace RezSaaS.Modules.Booking.Application;

public sealed class ExpireAppointmentRequestsService
{
    private readonly BookingDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public ExpireAppointmentRequestsService(
        BookingDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
        this.timeProvider = timeProvider;
    }

    public async Task<int> ExpireDueAsync(CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return 0;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        List<AppointmentRequest> dueRequests = await dbContext.AppointmentRequests
            .Where(entity => entity.Status == AppointmentRequestStatus.PendingApproval)
            .Where(entity => entity.ExpiresAtUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (AppointmentRequest request in dueRequests)
        {
            request.Expire();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return dueRequests.Count;
    }
}
