using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Abuse;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Infrastructure.Abuse;

public sealed class AdminUserBookingRestrictionEvaluator : IUserBookingRestrictionEvaluator
{
    private readonly AdminDbContext dbContext;

    public AdminUserBookingRestrictionEvaluator(AdminDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UserBookingRestriction> EvaluateAsync(
        Guid userAccountId,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty)
        {
            return UserBookingRestriction.None;
        }

        UserSanction? sanction = await dbContext.UserSanctions
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == userAccountId
                && entity.Type != UserSanctionType.Warning
                && entity.RevokedAtUtc == null
                && entity.StartsAtUtc <= evaluatedAtUtc
                && (entity.EndsAtUtc == null || entity.EndsAtUtc > evaluatedAtUtc))
            .OrderByDescending(entity => entity.Type == UserSanctionType.PermanentClosure)
            .ThenByDescending(entity => entity.Type == UserSanctionType.TemporaryBan)
            .ThenByDescending(entity => entity.StartsAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return sanction is null
            ? UserBookingRestriction.None
            : UserBookingRestriction.Restricted(
                sanction.Type.ToString(),
                sanction.EndsAtUtc);
    }
}
