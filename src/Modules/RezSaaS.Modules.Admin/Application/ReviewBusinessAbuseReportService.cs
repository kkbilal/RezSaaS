using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class ReviewBusinessAbuseReportService
{
    private const string AlreadyReviewed = "BUSINESS_ABUSE_REPORT_ALREADY_REVIEWED";
    private const string InvalidRequest = "BUSINESS_ABUSE_REPORT_REVIEW_INVALID";
    private const int MaxReasonLength = 300;
    private const string NotFound = "BUSINESS_ABUSE_REPORT_NOT_FOUND";
    private const string StrikeMissing = "BUSINESS_ABUSE_REPORT_STRIKE_MISSING";

    private readonly AbuseRiskOptions options;
    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public ReviewBusinessAbuseReportService(
        AdminDbContext dbContext,
        IOptions<AbuseRiskOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<ReviewBusinessAbuseReportResult> ReviewAsync(
        ReviewBusinessAbuseReportCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(command))
        {
            return ReviewBusinessAbuseReportResult.Failure(InvalidRequest);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        BusinessAbuseReport? report = await LockReportAsync(command.ReportId, cancellationToken);

        if (report is null)
        {
            return ReviewBusinessAbuseReportResult.Failure(NotFound);
        }

        if (report.Status == command.Decision)
        {
            Guid? existingStrikeId = command.Decision == AbuseReportStatus.Confirmed
                ? await FindStrikeIdAsync(report.Id, cancellationToken)
                : null;

            if (command.Decision == AbuseReportStatus.Confirmed
                && existingStrikeId is null)
            {
                return ReviewBusinessAbuseReportResult.Failure(StrikeMissing);
            }

            return ReviewBusinessAbuseReportResult.Success(report.Id, existingStrikeId);
        }

        if (report.Status != AbuseReportStatus.PendingReview)
        {
            return ReviewBusinessAbuseReportResult.Failure(AlreadyReviewed);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        report.Review(
            command.Decision,
            command.ActorUserAccountId,
            command.Reason,
            now);

        UserStrike? strike = null;

        if (command.Decision == AbuseReportStatus.Confirmed)
        {
            strike = UserStrike.Create(
                report.ReportedUserAccountId,
                report.TenantId,
                report.Id,
                report.ReasonCode,
                command.ActorUserAccountId,
                now,
                now.AddDays(options.StrikeLifetimeDays));
            dbContext.UserStrikes.Add(strike);
            dbContext.AbuseEvents.Add(
                AbuseEvent.Create(
                    report.TenantId,
                    report.ReportedUserAccountId,
                    "business.abuse_report_confirmed",
                    AbuseEventSeverity.Medium,
                    JsonSerializer.Serialize(
                        new
                        {
                            reportId = report.Id,
                            strikeId = strike.Id,
                            tenantId = report.TenantId,
                            reasonCode = report.ReasonCode.ToString(),
                        }),
                    now));
        }

        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                command.Decision == AbuseReportStatus.Confirmed
                    ? "BusinessAbuseReportConfirmed"
                    : "BusinessAbuseReportDismissed",
                JsonSerializer.Serialize(
                    new
                    {
                        reportId = report.Id,
                        strikeId = strike?.Id,
                        reportedUserAccountId = report.ReportedUserAccountId,
                        tenantId = report.TenantId,
                        reason = command.Reason.Trim(),
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ReviewBusinessAbuseReportResult.Success(report.Id, strike?.Id);
    }

    private async Task<Guid?> FindStrikeIdAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserStrikes
            .AsNoTracking()
            .Where(entity => entity.SourceAbuseReportId == reportId)
            .Select(entity => (Guid?)entity.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<BusinessAbuseReport?> LockReportAsync(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        return await dbContext.BusinessAbuseReports
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM admin."BusinessAbuseReports"
                WHERE "Id" = {reportId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static bool IsValid(ReviewBusinessAbuseReportCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && command.ReportId != Guid.Empty
            && command.Decision is AbuseReportStatus.Confirmed or AbuseReportStatus.Dismissed
            && !string.IsNullOrWhiteSpace(command.Reason)
            && command.Reason.Trim().Length <= MaxReasonLength;
    }
}
