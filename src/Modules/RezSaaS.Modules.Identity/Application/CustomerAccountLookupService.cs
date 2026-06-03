using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Security;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.Modules.Identity.Application;

public sealed class CustomerAccountLookupService
{
    private readonly IdentityDbContext dbContext;

    public CustomerAccountLookupService(IdentityDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<Guid, CustomerAccountMaskView>> GetMaskedProfilesAsync(
        IReadOnlyCollection<Guid> userAccountIds,
        CancellationToken cancellationToken = default)
    {
        if (userAccountIds.Count == 0)
        {
            return new Dictionary<Guid, CustomerAccountMaskView>();
        }

        Guid[] distinctUserAccountIds = userAccountIds
            .Where(entity => entity != Guid.Empty)
            .Distinct()
            .ToArray();

        List<CustomerAccountMaskSeed> accounts = await dbContext.Users
            .AsNoTracking()
            .Where(entity => distinctUserAccountIds.Contains(entity.Id))
            .Select(entity => new CustomerAccountMaskSeed(
                entity.Id,
                entity.Email ?? string.Empty,
                entity.PhoneNumber ?? string.Empty))
            .ToListAsync(cancellationToken);

        return accounts.ToDictionary(
            entity => entity.UserAccountId,
            entity => new CustomerAccountMaskView(
                entity.UserAccountId,
                PiiMasker.MaskEmail(entity.Email),
                PiiMasker.MaskPhone(entity.PhoneNumber)));
    }

    private sealed record CustomerAccountMaskSeed(
        Guid UserAccountId,
        string Email,
        string PhoneNumber);
}
