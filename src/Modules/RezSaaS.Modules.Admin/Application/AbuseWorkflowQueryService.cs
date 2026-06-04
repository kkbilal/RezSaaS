using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class AbuseWorkflowQueryService
{
    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public AbuseWorkflowQueryService(
        AdminDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public static bool IsValidAppealStatusOrEmpty(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            || TryParseAppealStatus(status, out _);
    }

    public static bool IsValidClosureStatusOrEmpty(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            || TryParseClosureStatus(status, out _);
    }

    public async Task<bool> HasActiveClosureCaseAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        return userAccountId != Guid.Empty
            && await dbContext.AccountClosureCases
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.UserAccountId == userAccountId
                        && (entity.Status == AccountClosureCaseStatus.PendingApproval
                            || entity.Status == AccountClosureCaseStatus.Approved
                            || entity.Status == AccountClosureCaseStatus.Executing),
                    cancellationToken);
    }

    public async Task<AbuseAppealView?> GetAppealByIdAsync(
        Guid appealId,
        CancellationToken cancellationToken = default)
    {
        return appealId == Guid.Empty
            ? null
            : await dbContext.AbuseAppeals
                .AsNoTracking()
                .Where(entity => entity.Id == appealId)
                .Select(ToAppealViewExpression())
                .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AccountClosureCaseView?> GetClosureCaseByIdAsync(
        Guid closureCaseId,
        CancellationToken cancellationToken = default)
    {
        return closureCaseId == Guid.Empty
            ? null
            : await dbContext.AccountClosureCases
                .AsNoTracking()
                .Where(entity => entity.Id == closureCaseId)
                .Select(ToClosureCaseViewExpression())
                .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerAbuseAppealView?> GetCustomerAppealByIdAsync(
        Guid userAccountId,
        Guid appealId,
        CancellationToken cancellationToken = default)
    {
        return userAccountId == Guid.Empty || appealId == Guid.Empty
            ? null
            : await dbContext.AbuseAppeals
                .AsNoTracking()
                .Where(entity => entity.Id == appealId
                    && entity.UserAccountId == userAccountId)
                .Select(entity => new CustomerAbuseAppealView(
                    entity.Id,
                    entity.TargetType,
                    entity.TargetId,
                    entity.Statement,
                    entity.Status,
                    entity.CreatedAtUtc,
                    entity.ReviewedAtUtc))
                .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AbuseAppealView>> GetAppealsAsync(
        Guid? userAccountId,
        string? status,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AbuseAppeal> query = dbContext.AbuseAppeals.AsNoTracking();

        if (userAccountId is { } userId)
        {
            query = query.Where(entity => entity.UserAccountId == userId);
        }

        if (TryParseAppealStatus(status, out AbuseAppealStatus parsedStatus))
        {
            query = query.Where(entity => entity.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(ToAppealViewExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AccountClosureCaseView>> GetClosureCasesAsync(
        Guid? userAccountId,
        string? status,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AccountClosureCase> query = dbContext.AccountClosureCases.AsNoTracking();

        if (userAccountId is { } userId)
        {
            query = query.Where(entity => entity.UserAccountId == userId);
        }

        if (TryParseClosureStatus(status, out AccountClosureCaseStatus parsedStatus))
        {
            query = query.Where(entity => entity.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(entity => entity.ProposedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(ToClosureCaseViewExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerAbuseOverviewView> GetCustomerOverviewAsync(
        Guid userAccountId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        int clampedTake = Math.Clamp(take, 1, 100);
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<CustomerUserSanctionView> sanctions = await dbContext.UserSanctions
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == userAccountId)
            .OrderByDescending(entity => entity.StartsAtUtc)
            .Take(clampedTake)
            .Select(entity => new CustomerUserSanctionView(
                entity.Id,
                entity.Type,
                entity.StartsAtUtc,
                entity.EndsAtUtc,
                entity.RevokedAtUtc,
                entity.Type != UserSanctionType.Warning
                    && entity.RevokedAtUtc == null
                    && entity.StartsAtUtc <= now
                    && (entity.EndsAtUtc == null || entity.EndsAtUtc > now)))
            .ToListAsync(cancellationToken);
        List<CustomerUserStrikeView> strikes = await dbContext.UserStrikes
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == userAccountId)
            .OrderByDescending(entity => entity.IssuedAtUtc)
            .Take(clampedTake)
            .Select(entity => new CustomerUserStrikeView(
                entity.Id,
                entity.ReasonCode,
                entity.IssuedAtUtc,
                entity.ExpiresAtUtc,
                entity.RevokedAtUtc))
            .ToListAsync(cancellationToken);
        List<CustomerAbuseAppealView> appeals = await dbContext.AbuseAppeals
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == userAccountId)
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .Take(clampedTake)
            .Select(entity => new CustomerAbuseAppealView(
                entity.Id,
                entity.TargetType,
                entity.TargetId,
                entity.Statement,
                entity.Status,
                entity.CreatedAtUtc,
                entity.ReviewedAtUtc))
            .ToListAsync(cancellationToken);
        List<CustomerAccountClosureCaseView> closureCases = await dbContext.AccountClosureCases
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == userAccountId)
            .OrderByDescending(entity => entity.ProposedAtUtc)
            .Take(clampedTake)
            .Select(entity => new CustomerAccountClosureCaseView(
                entity.Id,
                entity.CustomerNotice,
                entity.ProposedAtUtc,
                entity.EligibleForExecutionAtUtc,
                entity.Status,
                entity.DecidedAtUtc,
                entity.ExecutedAtUtc))
            .ToListAsync(cancellationToken);

        return new CustomerAbuseOverviewView(
            userAccountId,
            sanctions,
            strikes,
            appeals,
            closureCases);
    }

    private static System.Linq.Expressions.Expression<Func<AbuseAppeal, AbuseAppealView>>
        ToAppealViewExpression()
    {
        return entity => new AbuseAppealView(
            entity.Id,
            entity.UserAccountId,
            entity.TargetType,
            entity.TargetId,
            entity.Statement,
            entity.Status,
            entity.CreatedAtUtc,
            entity.ReviewedAtUtc,
            entity.ReviewedByUserAccountId,
            entity.ReviewReason);
    }

    private static System.Linq.Expressions.Expression<Func<AccountClosureCase, AccountClosureCaseView>>
        ToClosureCaseViewExpression()
    {
        return entity => new AccountClosureCaseView(
            entity.Id,
            entity.UserAccountId,
            entity.ProposedByUserAccountId,
            entity.InternalReason,
            entity.CustomerNotice,
            entity.ProposedAtUtc,
            entity.EligibleForExecutionAtUtc,
            entity.Status,
            entity.ReviewedByUserAccountId,
            entity.DecisionReason,
            entity.DecidedAtUtc,
            entity.ExecutionStartedByUserAccountId,
            entity.ExecutionStartedAtUtc,
            entity.ExecutedByUserAccountId,
            entity.ExecutedAtUtc);
    }

    private static bool TryParseAppealStatus(
        string? status,
        out AbuseAppealStatus parsedStatus)
    {
        return Enum.TryParse(status, ignoreCase: true, out parsedStatus)
            && Enum.IsDefined(parsedStatus);
    }

    private static bool TryParseClosureStatus(
        string? status,
        out AccountClosureCaseStatus parsedStatus)
    {
        return Enum.TryParse(status, ignoreCase: true, out parsedStatus)
            && Enum.IsDefined(parsedStatus);
    }
}
