using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class RevokeUserStrikeService
{
    private const string InvalidRequest = "USER_STRIKE_REVOCATION_INVALID";
    private const int MaxReasonLength = 300;
    private const string NotFound = "USER_STRIKE_NOT_FOUND";

    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public RevokeUserStrikeService(
        AdminDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<UserStrikeCommandResult> RevokeAsync(
        RevokeUserStrikeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ActorUserAccountId == Guid.Empty
            || command.UserAccountId == Guid.Empty
            || command.StrikeId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Reason)
            || command.Reason.Trim().Length > MaxReasonLength)
        {
            return UserStrikeCommandResult.Failure(InvalidRequest);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AbuseUserWorkflowLock.AcquireAsync(
            dbContext,
            command.UserAccountId,
            cancellationToken);
        UserStrike? strike = await LockStrikeAsync(
            command.UserAccountId,
            command.StrikeId,
            cancellationToken);

        if (strike is null)
        {
            return UserStrikeCommandResult.Failure(NotFound);
        }

        if (strike.RevokedAtUtc is not null)
        {
            return UserStrikeCommandResult.Success(strike.Id);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        strike.Revoke(command.ActorUserAccountId, command.Reason, now);
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                "UserStrikeRevoked",
                JsonSerializer.Serialize(
                    new
                    {
                        strikeId = strike.Id,
                        userAccountId = strike.UserAccountId,
                        sourceAbuseReportId = strike.SourceAbuseReportId,
                        reason = command.Reason.Trim(),
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return UserStrikeCommandResult.Success(strike.Id);
    }

    private async Task<UserStrike?> LockStrikeAsync(
        Guid userAccountId,
        Guid strikeId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserStrikes
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM admin."UserStrikes"
                WHERE "Id" = {strikeId}
                    AND "UserAccountId" = {userAccountId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
