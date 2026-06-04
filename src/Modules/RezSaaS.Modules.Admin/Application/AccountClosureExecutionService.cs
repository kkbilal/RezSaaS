using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class AccountClosureExecutionService
{
    private const string AppealPending = "ACCOUNT_CLOSURE_APPEAL_PENDING";
    private const string ExecutionDisabled = "ACCOUNT_CLOSURE_EXECUTION_DISABLED";
    private const string InvalidRequest = "ACCOUNT_CLOSURE_EXECUTION_INVALID";
    private const string NotApproved = "ACCOUNT_CLOSURE_NOT_APPROVED";
    private const string NotFound = "ACCOUNT_CLOSURE_NOT_FOUND";
    private const string RiskNoLongerHigh = "ACCOUNT_CLOSURE_RISK_NO_LONGER_HIGH";
    private const string WindowOpen = "ACCOUNT_CLOSURE_APPEAL_WINDOW_OPEN";

    private readonly AbuseRiskOptions options;
    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public AccountClosureExecutionService(
        AdminDbContext dbContext,
        IOptions<AbuseRiskOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<AbuseWorkflowCommandResult> BeginAsync(
        ExecuteAccountClosureCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ActorUserAccountId == Guid.Empty || command.ClosureCaseId == Guid.Empty)
        {
            return AbuseWorkflowCommandResult.Failure(InvalidRequest);
        }

        Guid? userAccountId = await dbContext.AccountClosureCases
            .AsNoTracking()
            .Where(entity => entity.Id == command.ClosureCaseId)
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
        AccountClosureCase? closureCase = await LockCaseAsync(
            command.ClosureCaseId,
            cancellationToken);

        if (closureCase is null)
        {
            return AbuseWorkflowCommandResult.Failure(NotFound);
        }

        if (closureCase.Status is AccountClosureCaseStatus.Executing
            or AccountClosureCaseStatus.Executed)
        {
            return AbuseWorkflowCommandResult.Success(closureCase.Id);
        }

        if (!options.AccountClosureExecutionEnabled)
        {
            return AbuseWorkflowCommandResult.Failure(ExecutionDisabled);
        }

        if (closureCase.Status != AccountClosureCaseStatus.Approved)
        {
            return AbuseWorkflowCommandResult.Failure(NotApproved);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (now < closureCase.EligibleForExecutionAtUtc)
        {
            return AbuseWorkflowCommandResult.Failure(WindowOpen);
        }

        int activeStrikeCount = await dbContext.UserStrikes
            .AsNoTracking()
            .CountAsync(
                entity => entity.UserAccountId == closureCase.UserAccountId
                    && entity.RevokedAtUtc == null
                    && entity.ExpiresAtUtc > now,
                cancellationToken);

        if (activeStrikeCount < options.HighStrikeThreshold)
        {
            return AbuseWorkflowCommandResult.Failure(RiskNoLongerHigh);
        }

        if (await dbContext.AbuseAppeals
            .AsNoTracking()
            .AnyAsync(
                entity => entity.UserAccountId == closureCase.UserAccountId
                    && entity.Status == AbuseAppealStatus.PendingReview,
                cancellationToken))
        {
            return AbuseWorkflowCommandResult.Failure(AppealPending);
        }

        closureCase.BeginExecution(command.ActorUserAccountId, now);
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                "AccountClosureExecutionStarted",
                JsonSerializer.Serialize(
                    new
                    {
                        closureCaseId = closureCase.Id,
                        userAccountId = closureCase.UserAccountId,
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AbuseWorkflowCommandResult.Success(closureCase.Id);
    }

    public async Task<AbuseWorkflowCommandResult> CompleteAsync(
        ExecuteAccountClosureCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ActorUserAccountId == Guid.Empty || command.ClosureCaseId == Guid.Empty)
        {
            return AbuseWorkflowCommandResult.Failure(InvalidRequest);
        }

        Guid? userAccountId = await dbContext.AccountClosureCases
            .AsNoTracking()
            .Where(entity => entity.Id == command.ClosureCaseId)
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
        AccountClosureCase? closureCase = await LockCaseAsync(
            command.ClosureCaseId,
            cancellationToken);

        if (closureCase is null)
        {
            return AbuseWorkflowCommandResult.Failure(NotFound);
        }

        if (closureCase.Status == AccountClosureCaseStatus.Executed)
        {
            return AbuseWorkflowCommandResult.Success(closureCase.Id);
        }

        if (closureCase.Status != AccountClosureCaseStatus.Executing)
        {
            return AbuseWorkflowCommandResult.Failure(NotApproved);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        List<UserSanction> activeSanctions = await dbContext.UserSanctions
            .Where(entity => entity.UserAccountId == closureCase.UserAccountId
                && entity.Type != UserSanctionType.Warning
                && entity.RevokedAtUtc == null
                && entity.StartsAtUtc <= now
                && (entity.EndsAtUtc == null || entity.EndsAtUtc > now))
            .ToListAsync(cancellationToken);

        foreach (UserSanction sanction in activeSanctions)
        {
            sanction.Revoke(
                command.ActorUserAccountId,
                "Superseded by permanent account closure.",
                now);
        }

        dbContext.UserSanctions.Add(
            UserSanction.Create(
                closureCase.UserAccountId,
                UserSanctionType.PermanentClosure,
                $"Account closure case {closureCase.Id:D}",
                now));
        closureCase.CompleteExecution(command.ActorUserAccountId, now);
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                "AccountClosureExecuted",
                JsonSerializer.Serialize(
                    new
                    {
                        closureCaseId = closureCase.Id,
                        userAccountId = closureCase.UserAccountId,
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
}
