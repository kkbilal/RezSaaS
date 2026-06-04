using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class CreateAbuseAppealService
{
    private const string AppealWindowClosed = "ABUSE_APPEAL_WINDOW_CLOSED";
    private const string InvalidRequest = "ABUSE_APPEAL_INVALID";
    private const int MaxStatementLength = 1000;
    private const string OpenAppealLimitExceeded = "ABUSE_APPEAL_OPEN_LIMIT_EXCEEDED";
    private const string TargetNotFound = "ABUSE_APPEAL_TARGET_NOT_FOUND";

    private readonly AbuseRiskOptions options;
    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public CreateAbuseAppealService(
        AdminDbContext dbContext,
        IOptions<AbuseRiskOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<AbuseWorkflowCommandResult> CreateAsync(
        CreateAbuseAppealCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(command))
        {
            return AbuseWorkflowCommandResult.Failure(InvalidRequest);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AbuseUserWorkflowLock.AcquireAsync(
            dbContext,
            command.UserAccountId,
            cancellationToken);

        Guid? existingAppealId = await dbContext.AbuseAppeals
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == command.UserAccountId
                && entity.TargetType == command.TargetType
                && entity.TargetId == command.TargetId)
            .Select(entity => (Guid?)entity.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingAppealId is not null)
        {
            return AbuseWorkflowCommandResult.Success(existingAppealId.Value, created: false);
        }

        if (await dbContext.AccountClosureCases
            .AsNoTracking()
            .AnyAsync(
                entity => entity.UserAccountId == command.UserAccountId
                    && (entity.Status == AccountClosureCaseStatus.Executing
                        || entity.Status == AccountClosureCaseStatus.Executed),
                cancellationToken))
        {
            return AbuseWorkflowCommandResult.Failure(AppealWindowClosed);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (!await TargetIsAppealableAsync(command, now, cancellationToken))
        {
            return AbuseWorkflowCommandResult.Failure(TargetNotFound);
        }

        int openAppealCount = await dbContext.AbuseAppeals
            .AsNoTracking()
            .CountAsync(
                entity => entity.UserAccountId == command.UserAccountId
                    && entity.Status == AbuseAppealStatus.PendingReview,
                cancellationToken);

        if (openAppealCount >= options.MaxOpenAppealsPerUser)
        {
            return AbuseWorkflowCommandResult.Failure(OpenAppealLimitExceeded);
        }

        AbuseAppeal appeal = AbuseAppeal.Create(
            command.UserAccountId,
            command.TargetType,
            command.TargetId,
            command.Statement,
            now);
        dbContext.AbuseAppeals.Add(appeal);
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.UserAccountId,
                "AbuseAppealCreated",
                JsonSerializer.Serialize(
                    new
                    {
                        appealId = appeal.Id,
                        userAccountId = appeal.UserAccountId,
                        targetType = appeal.TargetType.ToString(),
                        targetId = appeal.TargetId,
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AbuseWorkflowCommandResult.Success(appeal.Id, created: true);
    }

    private async Task<bool> TargetIsAppealableAsync(
        CreateAbuseAppealCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return command.TargetType switch
        {
            AbuseAppealTargetType.UserStrike => await dbContext.UserStrikes
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.Id == command.TargetId
                        && entity.UserAccountId == command.UserAccountId
                        && entity.RevokedAtUtc == null,
                    cancellationToken),
            AbuseAppealTargetType.UserSanction => await dbContext.UserSanctions
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.Id == command.TargetId
                        && entity.UserAccountId == command.UserAccountId
                        && entity.Type != UserSanctionType.Warning
                        && entity.RevokedAtUtc == null
                        && entity.StartsAtUtc <= now
                        && (entity.EndsAtUtc == null || entity.EndsAtUtc > now),
                    cancellationToken),
            AbuseAppealTargetType.AccountClosureCase => await dbContext.AccountClosureCases
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.Id == command.TargetId
                        && entity.UserAccountId == command.UserAccountId
                        && (entity.Status == AccountClosureCaseStatus.PendingApproval
                            || entity.Status == AccountClosureCaseStatus.Approved),
                    cancellationToken),
            _ => false,
        };
    }

    private static bool IsValid(CreateAbuseAppealCommand command)
    {
        return command.UserAccountId != Guid.Empty
            && command.TargetId != Guid.Empty
            && Enum.IsDefined(command.TargetType)
            && !string.IsNullOrWhiteSpace(command.Statement)
            && command.Statement.Trim().Length <= MaxStatementLength;
    }
}
