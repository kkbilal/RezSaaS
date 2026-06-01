using Microsoft.AspNetCore.Identity;

namespace RezSaaS.Modules.Identity.Domain;

public static class PlatformRoles
{
    public const string PlatformAdmin = nameof(PlatformAdmin);
    public const string PlatformSupport = nameof(PlatformSupport);

    private static readonly Guid PlatformAdminId = Guid.Parse("4d40e226-60da-4eeb-9c18-5601221fcf62");
    private static readonly Guid PlatformSupportId = Guid.Parse("855b4be9-9e11-444f-9aa1-6d980f2921a9");

    public static IEnumerable<IdentityRole<Guid>> CreateSeedData()
    {
        yield return CreateRole(PlatformAdminId, PlatformAdmin);
        yield return CreateRole(PlatformSupportId, PlatformSupport);
    }

    private static IdentityRole<Guid> CreateRole(Guid id, string name)
    {
        return new IdentityRole<Guid>
        {
            ConcurrencyStamp = id.ToString(),
            Id = id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
        };
    }
}
