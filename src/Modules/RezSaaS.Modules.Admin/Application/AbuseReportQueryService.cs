using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class AbuseReportQueryService
{
    private readonly AbuseRiskOptions options;
    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public AbuseReportQueryService(
        AdminDbContext dbContext,
        IOptions<AbuseRiskOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public static bool IsValidStatusOrEmpty(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            || TryParseStatus(status, out _);
    }

    public async Task<BusinessAbuseReportView?> GetByIdAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        if (reportId == Guid.Empty)
        {
            return null;
        }

        return await dbContext.BusinessAbuseReports
            .AsNoTracking()
            .Where(entity => entity.Id == reportId)
            .Select(ToReportViewExpression())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BusinessAbuseReportView>> GetReportsAsync(
        AbuseReportControlPlaneQuery reportQuery,
        CancellationToken cancellationToken = default)
    {
        IQueryable<BusinessAbuseReport> query = dbContext.BusinessAbuseReports.AsNoTracking();

        if (reportQuery.UserAccountId is { } userAccountId)
        {
            query = query.Where(entity => entity.ReportedUserAccountId == userAccountId);
        }

        if (reportQuery.TenantId is { } tenantId)
        {
            query = query.Where(entity => entity.TenantId == tenantId);
        }

        if (TryParseStatus(reportQuery.Status, out AbuseReportStatus status))
        {
            query = query.Where(entity => entity.Status == status);
        }

        return await query
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .Take(Math.Clamp(reportQuery.Take, 1, 100))
            .Select(ToReportViewExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserStrikeView>> GetUserStrikesAsync(
        Guid userAccountId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return await dbContext.UserStrikes
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == userAccountId)
            .OrderByDescending(entity => entity.IssuedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .Select(entity => new UserStrikeView(
                entity.Id,
                entity.UserAccountId,
                entity.TenantId,
                entity.SourceAbuseReportId,
                entity.ReasonCode,
                entity.IssuedByUserAccountId,
                entity.IssuedAtUtc,
                entity.ExpiresAtUtc,
                entity.RevokedAtUtc,
                entity.RevokedByUserAccountId,
                entity.RevocationReason,
                entity.RevokedAtUtc == null && entity.ExpiresAtUtc > now))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserStrikeView?> GetStrikeByIdAsync(
        Guid strikeId,
        CancellationToken cancellationToken = default)
    {
        if (strikeId == Guid.Empty)
        {
            return null;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        return await dbContext.UserStrikes
            .AsNoTracking()
            .Where(entity => entity.Id == strikeId)
            .Select(entity => new UserStrikeView(
                entity.Id,
                entity.UserAccountId,
                entity.TenantId,
                entity.SourceAbuseReportId,
                entity.ReasonCode,
                entity.IssuedByUserAccountId,
                entity.IssuedAtUtc,
                entity.ExpiresAtUtc,
                entity.RevokedAtUtc,
                entity.RevokedByUserAccountId,
                entity.RevocationReason,
                entity.RevokedAtUtc == null && entity.ExpiresAtUtc > now))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserRiskSummaryView> GetUserRiskSummaryAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        int activeStrikeCount = await dbContext.UserStrikes
            .AsNoTracking()
            .CountAsync(
                entity => entity.UserAccountId == userAccountId
                    && entity.RevokedAtUtc == null
                    && entity.ExpiresAtUtc > now,
                cancellationToken);

        UserRiskLevel level = activeStrikeCount switch
        {
            0 => UserRiskLevel.Normal,
            _ when activeStrikeCount >= options.HighStrikeThreshold => UserRiskLevel.High,
            _ when activeStrikeCount >= options.ElevatedStrikeThreshold => UserRiskLevel.Elevated,
            _ => UserRiskLevel.Monitor,
        };

        return new UserRiskSummaryView(activeStrikeCount, level);
    }

    private static System.Linq.Expressions.Expression<Func<BusinessAbuseReport, BusinessAbuseReportView>>
        ToReportViewExpression()
    {
        return entity => new BusinessAbuseReportView(
            entity.Id,
            entity.TenantId,
            entity.BranchId,
            entity.AppointmentRequestId,
            entity.ReportedUserAccountId,
            entity.ReportedByUserAccountId,
            entity.ReasonCode,
            entity.Note,
            entity.Status,
            entity.CreatedAtUtc,
            entity.ReviewedAtUtc,
            entity.ReviewedByUserAccountId,
            entity.ReviewReason);
    }

    private static bool TryParseStatus(
        string? status,
        out AbuseReportStatus parsedStatus)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = default;
            return false;
        }

        return Enum.TryParse(status, ignoreCase: true, out parsedStatus)
            && Enum.IsDefined(parsedStatus);
    }
}
