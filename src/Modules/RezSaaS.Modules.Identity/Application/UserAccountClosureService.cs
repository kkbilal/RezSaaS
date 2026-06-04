using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.Modules.Identity.Application;

public sealed class UserAccountClosureService
{
    private const string HasPlatformRole = "USER_ACCOUNT_CLOSURE_HAS_PLATFORM_ROLE";
    private const string InvalidRequest = "USER_ACCOUNT_CLOSURE_INVALID";
    private const string NotActive = "USER_ACCOUNT_CLOSURE_NOT_ACTIVE";
    private const string NotFound = "USER_ACCOUNT_CLOSURE_NOT_FOUND";

    private readonly IdentityDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public UserAccountClosureService(
        IdentityDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<UserAccountClosureEligibilityView?> GetEligibilityAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            return null;
        }

        AccountStatus? status = await dbContext.Users
            .AsNoTracking()
            .Where(entity => entity.Id == userAccountId)
            .Select(entity => (AccountStatus?)entity.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (status is null)
        {
            return null;
        }

        return new UserAccountClosureEligibilityView(
            userAccountId,
            status.Value,
            await HasPlatformRoleAsync(userAccountId, cancellationToken));
    }

    public async Task<UserAccountClosureResult> CloseAsync(
        Guid actorUserAccountId,
        Guid userAccountId,
        Guid closureCaseId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserAccountId == Guid.Empty
            || userAccountId == Guid.Empty
            || closureCaseId == Guid.Empty
            || actorUserAccountId == userAccountId)
        {
            return UserAccountClosureResult.Failure(InvalidRequest);
        }

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        UserAccount? userAccount = await dbContext.Users
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM identity."AspNetUsers"
                WHERE "Id" = {userAccountId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);

        if (userAccount is null)
        {
            return UserAccountClosureResult.Failure(NotFound);
        }

        if (await HasPlatformRoleAsync(userAccountId, cancellationToken))
        {
            return UserAccountClosureResult.Failure(HasPlatformRole);
        }

        if (userAccount.Status == AccountStatus.Closed)
        {
            return UserAccountClosureResult.Success(alreadyClosed: true);
        }

        if (userAccount.Status != AccountStatus.Active)
        {
            return UserAccountClosureResult.Failure(NotActive);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        userAccount.Close();
        userAccount.SecurityStamp = Guid.NewGuid().ToString("N");
        userAccount.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        dbContext.IdentityAuditLogEntries.Add(
            IdentityAuditLogEntry.Create(
                actorUserAccountId,
                userAccountId,
                "UserAccountClosed",
                JsonSerializer.Serialize(new { closureCaseId }),
                now));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return UserAccountClosureResult.Success();
    }

    private async Task<bool> HasPlatformRoleAsync(
        Guid userAccountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(entity => entity.UserId == userAccountId, cancellationToken);
    }
}
