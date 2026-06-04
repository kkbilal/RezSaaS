using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class CreateBusinessAbuseReportService
{
    private const string DailyLimitExceeded = "BUSINESS_ABUSE_REPORT_DAILY_LIMIT_EXCEEDED";
    private const string InvalidRequest = "BUSINESS_ABUSE_REPORT_INVALID";
    private const int MaxNoteLength = 300;

    private readonly AbuseRiskOptions options;
    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public CreateBusinessAbuseReportService(
        AdminDbContext dbContext,
        IOptions<AbuseRiskOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<BusinessAbuseReportCommandResult> CreateAsync(
        CreateBusinessAbuseReportCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(command))
        {
            return BusinessAbuseReportCommandResult.Failure(InvalidRequest);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        string reportLockKey =
            $"business-abuse-report:{command.TenantId:D}:{command.AppointmentRequestId:D}";
        string actorLockKey =
            $"business-abuse-reporter:{command.TenantId:D}:{command.ReportedByUserAccountId:D}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({reportLockKey}, 0))",
            cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({actorLockKey}, 0))",
            cancellationToken);

        Guid? existingReportId = await dbContext.BusinessAbuseReports
            .AsNoTracking()
            .Where(entity => entity.TenantId == command.TenantId
                && entity.AppointmentRequestId == command.AppointmentRequestId)
            .Select(entity => (Guid?)entity.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingReportId is not null)
        {
            return BusinessAbuseReportCommandResult.Success(existingReportId.Value, created: false);
        }

        DateTimeOffset dailyWindowStart = now.AddDays(-1);
        int dailyReportCount = await dbContext.BusinessAbuseReports
            .AsNoTracking()
            .CountAsync(
                entity => entity.TenantId == command.TenantId
                    && entity.ReportedByUserAccountId == command.ReportedByUserAccountId
                    && entity.CreatedAtUtc >= dailyWindowStart,
                cancellationToken);

        if (dailyReportCount >= options.MaxBusinessReportsPerActorPerDay)
        {
            dbContext.AbuseEvents.Add(
                AbuseEvent.Create(
                    command.TenantId,
                    command.ReportedByUserAccountId,
                    "business.abuse_report_daily_limit_exceeded",
                    AbuseEventSeverity.Medium,
                    JsonSerializer.Serialize(
                        new
                        {
                            tenantId = command.TenantId,
                            actorUserAccountId = command.ReportedByUserAccountId,
                        }),
                    now));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return BusinessAbuseReportCommandResult.Failure(DailyLimitExceeded);
        }

        BusinessAbuseReport report = BusinessAbuseReport.Create(
            command.TenantId,
            command.BranchId,
            command.AppointmentRequestId,
            command.ReportedUserAccountId,
            command.ReportedByUserAccountId,
            command.ReasonCode,
            command.Note,
            now);
        dbContext.BusinessAbuseReports.Add(report);
        dbContext.AbuseEvents.Add(
            AbuseEvent.Create(
                report.TenantId,
                report.ReportedUserAccountId,
                "business.appointment_request_reported",
                AbuseEventSeverity.Low,
                JsonSerializer.Serialize(
                    new
                    {
                        reportId = report.Id,
                        tenantId = report.TenantId,
                        branchId = report.BranchId,
                        appointmentRequestId = report.AppointmentRequestId,
                        reasonCode = report.ReasonCode.ToString(),
                    }),
                now));
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                report.ReportedByUserAccountId,
                "BusinessAbuseReportCreated",
                JsonSerializer.Serialize(
                    new
                    {
                        reportId = report.Id,
                        tenantId = report.TenantId,
                        branchId = report.BranchId,
                        appointmentRequestId = report.AppointmentRequestId,
                        reportedUserAccountId = report.ReportedUserAccountId,
                        reasonCode = report.ReasonCode.ToString(),
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return BusinessAbuseReportCommandResult.Success(report.Id, created: true);
    }

    private static bool IsValid(CreateBusinessAbuseReportCommand command)
    {
        return command.TenantId != Guid.Empty
            && command.BranchId != Guid.Empty
            && command.AppointmentRequestId != Guid.Empty
            && command.ReportedUserAccountId != Guid.Empty
            && command.ReportedByUserAccountId != Guid.Empty
            && command.ReportedUserAccountId != command.ReportedByUserAccountId
            && Enum.IsDefined(command.ReasonCode)
            && (command.Note is null || command.Note.Trim().Length <= MaxNoteLength);
    }
}
