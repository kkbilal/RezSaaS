using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Application;

public sealed class ApplyUserSanctionService
{
    private const string ActiveSanctionExists = "USER_ACTIVE_SANCTION_EXISTS";
    private const string InvalidRequest = "USER_SANCTION_INVALID";
    private const int MaxReasonLength = 300;
    private const string PermanentClosureRequiresAccountWorkflow =
        "USER_PERMANENT_CLOSURE_REQUIRES_ACCOUNT_WORKFLOW";

    private readonly AdminDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public ApplyUserSanctionService(
        AdminDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<ApplyUserSanctionResult> ApplyAsync(
        ApplyUserSanctionCommand command,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        if (!IsValid(command, now))
        {
            return ApplyUserSanctionResult.Failure(InvalidRequest);
        }

        if (command.Type == UserSanctionType.PermanentClosure)
        {
            return ApplyUserSanctionResult.Failure(PermanentClosureRequiresAccountWorkflow);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({command.UserAccountId.ToString()}, 0))",
            cancellationToken);

        if (command.Type != UserSanctionType.Warning
            && await HasActiveBlockingSanctionAsync(
                command.UserAccountId,
                now,
                cancellationToken))
        {
            return ApplyUserSanctionResult.Failure(ActiveSanctionExists);
        }

        UserSanction sanction = UserSanction.Create(
            command.UserAccountId,
            command.Type,
            command.Reason,
            now,
            command.EndsAtUtc);
        dbContext.UserSanctions.Add(sanction);
        dbContext.AdminAuditLogEntries.Add(
            AdminAuditLogEntry.Create(
                command.ActorUserAccountId,
                "UserSanctionApplied",
                JsonSerializer.Serialize(
                    new
                    {
                        sanctionId = sanction.Id,
                        userAccountId = sanction.UserAccountId,
                        type = sanction.Type.ToString(),
                        reason = sanction.Reason,
                        endsAtUtc = sanction.EndsAtUtc,
                    }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ApplyUserSanctionResult.Success(sanction.Id);
    }

    private async Task<bool> HasActiveBlockingSanctionAsync(
        Guid userAccountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserSanctions
            .AsNoTracking()
            .AnyAsync(
                entity => entity.UserAccountId == userAccountId
                    && entity.Type != UserSanctionType.Warning
                    && entity.RevokedAtUtc == null
                    && entity.StartsAtUtc <= now
                    && (entity.EndsAtUtc == null || entity.EndsAtUtc > now),
                cancellationToken);
    }

    private static bool IsValid(
        ApplyUserSanctionCommand command,
        DateTimeOffset now)
    {
        if (command.ActorUserAccountId == Guid.Empty
            || command.UserAccountId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Reason)
            || command.Reason.Trim().Length > MaxReasonLength
            || !Enum.IsDefined(command.Type))
        {
            return false;
        }

        return command.Type switch
        {
            UserSanctionType.Warning => command.EndsAtUtc is null,
            UserSanctionType.Cooldown => command.EndsAtUtc > now
                && command.EndsAtUtc <= now.AddHours(24),
            UserSanctionType.TemporaryBan => command.EndsAtUtc >= now.AddHours(24)
                && command.EndsAtUtc <= now.AddHours(72),
            UserSanctionType.PermanentClosure => command.EndsAtUtc is null,
            _ => false,
        };
    }
}
