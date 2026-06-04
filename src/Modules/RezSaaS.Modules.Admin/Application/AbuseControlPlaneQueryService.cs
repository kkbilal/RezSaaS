using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class AbuseControlPlaneQueryService
{
    private readonly AbuseReportQueryService abuseReportQueryService;
    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public AbuseControlPlaneQueryService(
        AdminDbContext dbContext,
        AbuseReportQueryService abuseReportQueryService,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.abuseReportQueryService = abuseReportQueryService;
        this.timeProvider = timeProvider;
    }

    public static bool IsValidSeverityOrEmpty(string? severity)
    {
        return string.IsNullOrWhiteSpace(severity)
            || TryParseSeverity(severity, out _);
    }

    public async Task<IReadOnlyCollection<AbuseEventView>> GetEventsAsync(
        AbuseControlPlaneQuery eventQuery,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AbuseEvent> query = dbContext.AbuseEvents.AsNoTracking();

        if (eventQuery.UserAccountId is { } userAccountId)
        {
            query = query.Where(entity => entity.UserAccountId == userAccountId);
        }

        if (eventQuery.TenantId is { } tenantId)
        {
            query = query.Where(entity => entity.TenantId == tenantId);
        }

        if (TryParseSeverity(eventQuery.Severity, out AbuseEventSeverity severity))
        {
            query = query.Where(entity => entity.Severity == severity);
        }

        return await query
            .OrderByDescending(entity => entity.OccurredAtUtc)
            .Take(Math.Clamp(eventQuery.Take, 1, 100))
            .Select(entity => new AbuseEventView(
                entity.Id,
                entity.TenantId,
                entity.UserAccountId,
                entity.EventType,
                entity.Severity,
                entity.DetailsJson,
                entity.OccurredAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAbuseOverviewView?> GetUserOverviewAsync(
        Guid userAccountId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            return null;
        }

        int clampedTake = Math.Clamp(take, 1, 100);
        IReadOnlyCollection<AbuseEventView> events =
            await GetEventsAsync(
                new AbuseControlPlaneQuery(
                    userAccountId,
                    TenantId: null,
                    Severity: null,
                    clampedTake),
                cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<UserSanctionView> sanctions = await dbContext.UserSanctions
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == userAccountId)
            .OrderByDescending(entity => entity.StartsAtUtc)
            .Take(clampedTake)
            .Select(entity => new UserSanctionView(
                entity.Id,
                entity.UserAccountId,
                entity.Type,
                entity.Reason,
                entity.StartsAtUtc,
                entity.EndsAtUtc,
                entity.RevokedAtUtc,
                entity.RevokedByUserAccountId,
                entity.RevocationReason,
                entity.Type != UserSanctionType.Warning
                    && entity.RevokedAtUtc == null
                    && entity.StartsAtUtc <= now
                    && (entity.EndsAtUtc == null || entity.EndsAtUtc > now)))
            .ToListAsync(cancellationToken);
        IReadOnlyCollection<BusinessAbuseReportView> reports =
            await abuseReportQueryService.GetReportsAsync(
                new AbuseReportControlPlaneQuery(
                    userAccountId,
                    TenantId: null,
                    Status: null,
                    clampedTake),
                cancellationToken);
        IReadOnlyCollection<UserStrikeView> strikes =
            await abuseReportQueryService.GetUserStrikesAsync(
                userAccountId,
                clampedTake,
                cancellationToken);
        UserRiskSummaryView risk =
            await abuseReportQueryService.GetUserRiskSummaryAsync(
                userAccountId,
                cancellationToken);

        return new UserAbuseOverviewView(
            userAccountId,
            events,
            sanctions,
            reports,
            strikes,
            risk);
    }

    private static bool TryParseSeverity(
        string? severity,
        out AbuseEventSeverity parsedSeverity)
    {
        if (string.IsNullOrWhiteSpace(severity))
        {
            parsedSeverity = default;
            return false;
        }

        return Enum.TryParse(severity, ignoreCase: true, out parsedSeverity)
            && Enum.IsDefined(parsedSeverity);
    }
}
