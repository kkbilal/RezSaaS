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
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return 0;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        string pendingStatus = AppointmentRequestStatus.PendingApproval.ToString();

        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        List<AppointmentRequest> dueRequests = await dbContext.AppointmentRequests
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM booking."AppointmentRequests"
                WHERE "TenantId" = {tenantId}
                    AND "Status" = {pendingStatus}
                    AND "ExpiresAtUtc" <= {now}
                ORDER BY "ExpiresAtUtc"
                FOR UPDATE SKIP LOCKED
                """)
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        foreach (AppointmentRequest request in dueRequests)
        {
            request.Expire();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return dueRequests.Count;
    }
}
