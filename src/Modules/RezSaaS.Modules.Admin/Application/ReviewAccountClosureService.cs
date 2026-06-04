using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class ReviewAccountClosureService
{
    private const string AlreadyReviewed = "ACCOUNT_CLOSURE_ALREADY_REVIEWED";
    private const string InvalidRequest = "ACCOUNT_CLOSURE_REVIEW_INVALID";
    private const int MaxReasonLength = 500;
    private const string NotFound = "ACCOUNT_CLOSURE_NOT_FOUND";
    private const string ProposerCannotReview = "ACCOUNT_CLOSURE_REQUIRES_SECOND_ADMIN";

    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public ReviewAccountClosureService(
        AdminDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<AbuseWorkflowCommandResult> ReviewAsync(
        ReviewAccountClosureCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(command))
        {
            return AbuseWorkflowCommandResult.Failure(InvalidRequest);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        AccountClosureCase? closureCase = await LockCaseAsync(
            command.ClosureCaseId,
            cancellationToken);

        if (closureCase is null)
        {
            return AbuseWorkflowCommandResult.Failure(NotFound);
        }

        if (closureCase.ProposedByUserAccountId == command.ActorUserAccountId)
        {
            return AbuseWorkflowCommandResult.Failure(ProposerCannotReview);
        }

        if (closureCase.Status == command.Decision)
        {
            return AbuseWorkflowCommandResult.Success(closureCase.Id);
        }

        if (closureCase.Status != AccountClosureCaseStatus.PendingApproval)
        {
            return AbuseWorkflowCommandResult.Failure(AlreadyReviewed);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (command.Decision == AccountClosureCaseStatus.Approved)
        {
            closureCase.Approve(command.ActorUserAccountId, command.Reason, now);
        }
        else
        {
            closureCase.Reject(command.ActorUserAccountId, command.Reason, now);
        }

        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                command.Decision == AccountClosureCaseStatus.Approved
                    ? "AccountClosureApproved"
                    : "AccountClosureRejected",
                JsonSerializer.Serialize(
                    new
                    {
                        closureCaseId = closureCase.Id,
                        userAccountId = closureCase.UserAccountId,
                        proposedByUserAccountId = closureCase.ProposedByUserAccountId,
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AbuseWorkflowCommandResult.Success(closureCase.Id);
    }

    private async Task<AccountClosureCase?> LockCaseAsync(
        Guid closureCaseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AccountClosureCases
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM admin."AccountClosureCases"
                WHERE "Id" = {closureCaseId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static bool IsValid(ReviewAccountClosureCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && command.ClosureCaseId != Guid.Empty
            && command.Decision is AccountClosureCaseStatus.Approved or AccountClosureCaseStatus.Rejected
            && !string.IsNullOrWhiteSpace(command.Reason)
            && command.Reason.Trim().Length <= MaxReasonLength;
    }
}
