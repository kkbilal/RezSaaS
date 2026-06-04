using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class AccountClosureNoticeDeliveryService
{
    private readonly AbuseRiskOptions options;
    private readonly AdminDbContext dbContext;

    public AccountClosureNoticeDeliveryService(
        AdminDbContext dbContext,
        IOptions<AbuseRiskOptions> options)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
    }

    public async Task<AccountClosureNoticeDeliveryState> GetStateAsync(
        Guid closureCaseId,
        CancellationToken cancellationToken = default)
    {
        if (closureCaseId == Guid.Empty)
        {
            return AccountClosureNoticeDeliveryState.NotFound;
        }

        var closureCase = await dbContext.AccountClosureCases
            .AsNoTracking()
            .Where(entity => entity.Id == closureCaseId)
            .Select(entity => new
            {
                entity.CustomerNoticeDeliveredAtUtc,
                entity.Status,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (closureCase is null)
        {
            return AccountClosureNoticeDeliveryState.NotFound;
        }

        if (closureCase.CustomerNoticeDeliveredAtUtc is not null)
        {
            return AccountClosureNoticeDeliveryState.Delivered;
        }

        return closureCase.Status is AccountClosureCaseStatus.PendingApproval
            or AccountClosureCaseStatus.Approved
            ? AccountClosureNoticeDeliveryState.Required
            : AccountClosureNoticeDeliveryState.NoLongerRequired;
    }

    public async Task<AccountClosureNoticeDeliveryState> MarkDeliveredAsync(
        Guid closureCaseId,
        DateTimeOffset deliveredAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (closureCaseId == Guid.Empty)
        {
            return AccountClosureNoticeDeliveryState.NotFound;
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        AccountClosureCase? closureCase = await dbContext.AccountClosureCases
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM admin."AccountClosureCases"
                WHERE "Id" = {closureCaseId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (closureCase is null)
        {
            return AccountClosureNoticeDeliveryState.NotFound;
        }

        if (closureCase.CustomerNoticeDeliveredAtUtc is not null)
        {
            return AccountClosureNoticeDeliveryState.Delivered;
        }

        if (closureCase.Status is not AccountClosureCaseStatus.PendingApproval
            and not AccountClosureCaseStatus.Approved)
        {
            return AccountClosureNoticeDeliveryState.NoLongerRequired;
        }

        closureCase.MarkCustomerNoticeDelivered(
            deliveredAtUtc,
            TimeSpan.FromDays(options.ClosureAppealWindowDays));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AccountClosureNoticeDeliveryState.Delivered;
    }
}
