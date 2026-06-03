using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.Modules.Identity.Application;

public sealed class UserAccountExistenceService
{
    private readonly IdentityDbContext dbContext;

    public UserAccountExistenceService(IdentityDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> ExistsActiveAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        return userAccountId != Guid.Empty
            && await dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    entity => entity.Id == userAccountId
                        && entity.Status == AccountStatus.Active,
                    cancellationToken);
    }
}
