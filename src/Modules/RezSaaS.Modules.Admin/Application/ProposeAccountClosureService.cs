using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class ProposeAccountClosureService
{
    private const string ActiveCaseExists = "ACCOUNT_CLOSURE_ACTIVE_CASE_EXISTS";
    private const string HighRiskRequired = "ACCOUNT_CLOSURE_HIGH_RISK_REQUIRED";
    private const string InvalidRequest = "ACCOUNT_CLOSURE_PROPOSAL_INVALID";
    private const int MaxTextLength = 500;

    private readonly AbuseRiskOptions options;
    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public ProposeAccountClosureService(
        AdminDbContext dbContext,
        IOptions<AbuseRiskOptions> options,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<AbuseWorkflowCommandResult> ProposeAsync(
        ProposeAccountClosureCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!IsValid(command))
        {
            return AbuseWorkflowCommandResult.Failure(InvalidRequest);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AbuseUserWorkflowLock.AcquireAsync(
            dbContext,
            command.UserAccountId,
            cancellationToken);

        var activeCase = await dbContext.AccountClosureCases
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == command.UserAccountId
                && (entity.Status == AccountClosureCaseStatus.PendingApproval
                    || entity.Status == AccountClosureCaseStatus.Approved
                    || entity.Status == AccountClosureCaseStatus.Executing))
            .Select(entity => new
            {
                entity.Id,
                entity.ProposedByUserAccountId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCase is not null)
        {
            if (activeCase.ProposedByUserAccountId == command.ActorUserAccountId)
            {
                return AbuseWorkflowCommandResult.Success(activeCase.Id, created: false);
            }

            return AbuseWorkflowCommandResult.Failure(ActiveCaseExists);
        }

        int activeStrikeCount = await dbContext.UserStrikes
            .AsNoTracking()
            .CountAsync(
                entity => entity.UserAccountId == command.UserAccountId
                    && entity.RevokedAtUtc == null
                    && entity.ExpiresAtUtc > now,
                cancellationToken);

        if (activeStrikeCount < options.HighStrikeThreshold)
        {
            return AbuseWorkflowCommandResult.Failure(HighRiskRequired);
        }

        AccountClosureCase closureCase = AccountClosureCase.Create(
            command.UserAccountId,
            command.ActorUserAccountId,
            command.InternalReason,
            command.CustomerNotice,
            now);
        dbContext.AccountClosureCases.Add(closureCase);
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                "AccountClosureProposed",
                JsonSerializer.Serialize(
                    new
                    {
                        closureCaseId = closureCase.Id,
                        userAccountId = closureCase.UserAccountId,
                        activeStrikeCount,
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AbuseWorkflowCommandResult.Success(closureCase.Id, created: true);
    }

    private static bool IsValid(ProposeAccountClosureCommand command)
    {
        return command.ActorUserAccountId != Guid.Empty
            && command.UserAccountId != Guid.Empty
            && command.ActorUserAccountId != command.UserAccountId
            && !string.IsNullOrWhiteSpace(command.InternalReason)
            && command.InternalReason.Trim().Length <= MaxTextLength
            && !string.IsNullOrWhiteSpace(command.CustomerNotice)
            && command.CustomerNotice.Trim().Length <= MaxTextLength;
    }
}
