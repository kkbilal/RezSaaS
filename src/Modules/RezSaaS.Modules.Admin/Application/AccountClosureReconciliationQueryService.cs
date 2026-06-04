using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class AccountClosureReconciliationQueryService
{
    private readonly AdminDbContext dbContext;

    public AccountClosureReconciliationQueryService(AdminDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<AccountClosureReconciliationSnapshot> InspectAsync(
        AccountClosureReconciliationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(query.SampleSize);
        int sampleSize = Math.Min(query.SampleSize, 100);
        IQueryable<AccountClosureCase> closureCases =
            dbContext.AccountClosureCases.AsNoTracking();
        IQueryable<AccountClosureCase> notificationOverdue = closureCases
            .Where(entity => (entity.Status == AccountClosureCaseStatus.PendingApproval
                    || entity.Status == AccountClosureCaseStatus.Approved)
                && entity.CustomerNoticeDeliveredAtUtc == null
                && entity.ProposedAtUtc <= query.NotificationOverdueBeforeUtc);
        IQueryable<AccountClosureCase> executionStalled = closureCases
            .Where(entity => entity.Status == AccountClosureCaseStatus.Executing
                && entity.ExecutionStartedAtUtc != null
                && entity.ExecutionStartedAtUtc <= query.ExecutionStalledBeforeUtc);

        int notificationOverdueCount = await notificationOverdue.CountAsync(cancellationToken);
        int executionStalledCount = await executionStalled.CountAsync(cancellationToken);
        List<Guid> notificationOverdueClosureCaseIds = await notificationOverdue
            .OrderBy(entity => entity.ProposedAtUtc)
            .Take(sampleSize)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);
        List<Guid> executionStalledClosureCaseIds = await executionStalled
            .OrderBy(entity => entity.ExecutionStartedAtUtc)
            .Take(sampleSize)
            .Select(entity => entity.Id)
            .ToListAsync(cancellationToken);

        return new AccountClosureReconciliationSnapshot(
            notificationOverdueCount,
            executionStalledCount,
            notificationOverdueClosureCaseIds,
            executionStalledClosureCaseIds);
    }
}
