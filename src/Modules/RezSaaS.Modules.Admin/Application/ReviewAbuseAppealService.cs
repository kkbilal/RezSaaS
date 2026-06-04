using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class ReviewAbuseAppealService
{
    private const string AlreadyReviewed = "ABUSE_APPEAL_ALREADY_REVIEWED";
    private const string InvalidRequest = "ABUSE_APPEAL_REVIEW_INVALID";
    private const int MaxReasonLength = 300;
    private const string NotFound = "ABUSE_APPEAL_NOT_FOUND";
    private const string SelfReviewForbidden = "ABUSE_APPEAL_SELF_REVIEW_FORBIDDEN";
    private const string TargetNotFound = "ABUSE_APPEAL_TARGET_NOT_FOUND";

    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public ReviewAbuseAppealService(
        AdminDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<AbuseWorkflowCommandResult> ReviewAsync(
        ReviewAbuseAppealCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(command))
        {
            return AbuseWorkflowCommandResult.Failure(InvalidRequest);
        }

        Guid? userAccountId = await dbContext.AbuseAppeals
            .AsNoTracking()
            .Where(entity => entity.Id == command.AppealId)
            .Select(entity => (Guid?)entity.UserAccountId)
            .SingleOrDefaultAsync(cancellationToken);

        if (userAccountId is null)
        {
            return AbuseWorkflowCommandResult.Failure(NotFound);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AbuseUserWorkflowLock.AcquireAsync(
            dbContext,
            userAccountId.Value,
            cancellationToken);
        AbuseAppeal? appeal = await LockAppealAsync(command.AppealId, cancellationToken);

        if (appeal is null)
        {
            return AbuseWorkflowCommandResult.Failure(NotFound);
        }

        if (appeal.UserAccountId == command.ActorUserAccountId)
        {
            return AbuseWorkflowCommandResult.Failure(SelfReviewForbidden);
        }

        if (appeal.Status == command.Decision)
        {
            return AbuseWorkflowCommandResult.Success(appeal.Id);
        }

        if (appeal.Status != AbuseAppealStatus.PendingReview)
        {
            return AbuseWorkflowCommandResult.Failure(AlreadyReviewed);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (command.Decision == AbuseAppealStatus.Accepted
            && !await ApplyAcceptedAppealAsync(appeal, command, now, cancellationToken))
        {
            return AbuseWorkflowCommandResult.Failure(TargetNotFound);
        }

        appeal.Review(
            command.Decision,
            command.ActorUserAccountId,
            command.Reason,
            now);
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                command.Decision == AbuseAppealStatus.Accepted
                    ? "AbuseAppealAccepted"
                    : "AbuseAppealRejected",
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

        return AbuseWorkflowCommandResult.Success(appeal.Id);
    }

    private async Task<bool> ApplyAcceptedAppealAsync(
        AbuseAppeal appeal,
        ReviewAbuseAppealCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        switch (appeal.TargetType)
        {
            case AbuseAppealTargetType.UserStrike:
                UserStrike? strike = await dbContext.UserStrikes
                    .FromSqlInterpolated(
                        $"""
                        SELECT *
                        FROM admin."UserStrikes"
                        WHERE "Id" = {appeal.TargetId}
                            AND "UserAccountId" = {appeal.UserAccountId}
                        FOR UPDATE
                        """)
                    .SingleOrDefaultAsync(cancellationToken);

                if (strike is null)
                {
                    return false;
                }

                strike.Revoke(command.ActorUserAccountId, command.Reason, now);
                return true;

            case AbuseAppealTargetType.UserSanction:
                UserSanction? sanction = await dbContext.UserSanctions
                    .FromSqlInterpolated(
                        $"""
                        SELECT *
                        FROM admin."UserSanctions"
                        WHERE "Id" = {appeal.TargetId}
                            AND "UserAccountId" = {appeal.UserAccountId}
                        FOR UPDATE
                        """)
                    .SingleOrDefaultAsync(cancellationToken);

                if (sanction is null || sanction.Type == UserSanctionType.Warning)
                {
                    return false;
                }

                sanction.Revoke(command.ActorUserAccountId, command.Reason, now);
                return true;

            case AbuseAppealTargetType.AccountClosureCase:
                AccountClosureCase? closureCase = await dbContext.AccountClosureCases
                    .FromSqlInterpolated(
                        $"""
                        SELECT *
                        FROM admin."AccountClosureCases"
                        WHERE "Id" = {appeal.TargetId}
                            AND "UserAccountId" = {appeal.UserAccountId}
                        FOR UPDATE
                        """)
                    .SingleOrDefaultAsync(cancellationToken);

                if (closureCase is null
                    || closureCase.Status is not AccountClosureCaseStatus.PendingApproval
                        and not AccountClosureCaseStatus.Approved)
                {
                    return false;
                }

                closureCase.CancelByAcceptedAppeal(command.ActorUserAccountId, command.Reason, now);
                return true;

            default:
                return false;
        }
    }

    private async Task<AbuseAppeal?> LockAppealAsync(
        Guid appealId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AbuseAppeals
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM admin."AbuseAppeals"
                WHERE "Id" = {appealId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static bool IsValid(ReviewAbuseAppealCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && command.AppealId != Guid.Empty
            && command.Decision is AbuseAppealStatus.Accepted or AbuseAppealStatus.Rejected
            && !string.IsNullOrWhiteSpace(command.Reason)
            && command.Reason.Trim().Length <= MaxReasonLength;
    }
}
