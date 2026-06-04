using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class RevokeUserSanctionService
{
    private const string InvalidRequest = "USER_SANCTION_REVOCATION_INVALID";
    private const int MaxReasonLength = 300;
    private const string NotFound = "USER_SANCTION_NOT_FOUND";
    private const string NotRevocable = "USER_SANCTION_NOT_REVOCABLE";

    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public RevokeUserSanctionService(
        AdminDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<ApplyUserSanctionResult> RevokeAsync(
        RevokeUserSanctionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ActorUserAccountId == Guid.Empty
            || command.UserAccountId == Guid.Empty
            || command.SanctionId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Reason)
            || command.Reason.Trim().Length > MaxReasonLength)
        {
            return ApplyUserSanctionResult.Failure(InvalidRequest);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AbuseUserWorkflowLock.AcquireAsync(
            dbContext,
            command.UserAccountId,
            cancellationToken);
        UserSanction? sanction = await LockSanctionAsync(
            command.UserAccountId,
            command.SanctionId,
            cancellationToken);

        if (sanction is null)
        {
            return ApplyUserSanctionResult.Failure(NotFound);
        }

        if (sanction.RevokedAtUtc is not null)
        {
            return ApplyUserSanctionResult.Success(sanction.Id);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        if (sanction.Type == UserSanctionType.Warning
            || sanction.EndsAtUtc is not null && sanction.EndsAtUtc <= now)
        {
            return ApplyUserSanctionResult.Failure(NotRevocable);
        }

        sanction.Revoke(
            command.ActorUserAccountId,
            command.Reason,
            now);
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                "UserSanctionRevoked",
                JsonSerializer.Serialize(
                    new
                    {
                        sanctionId = sanction.Id,
                        userAccountId = sanction.UserAccountId,
                        type = sanction.Type.ToString(),
                        reason = command.Reason.Trim(),
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ApplyUserSanctionResult.Success(sanction.Id);
    }

    private async Task<UserSanction?> LockSanctionAsync(
        Guid userAccountId,
        Guid sanctionId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserSanctions
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM admin."UserSanctions"
                WHERE "Id" = {sanctionId}
                    AND "UserAccountId" = {userAccountId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
