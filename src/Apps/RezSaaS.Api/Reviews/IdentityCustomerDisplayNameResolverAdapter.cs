using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Reviews;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.Api.Reviews;

/// <summary>
/// Composition-root adapter that implements the Reviews cross-module contract
/// <see cref="ICustomerDisplayNameResolver"/>. Uses the Identity module's
/// <c>UserAccount</c> table (read-only) and returns a PII-safe display name:
/// user name when available, otherwise a masked email, otherwise null.
/// </summary>
public sealed class IdentityCustomerDisplayNameResolverAdapter : ICustomerDisplayNameResolver
{
    private readonly IdentityDbContext identityDbContext;

    public IdentityCustomerDisplayNameResolverAdapter(IdentityDbContext identityDbContext)
    {
        this.identityDbContext = identityDbContext;
    }

    public async Task<string?> ResolveAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default)
    {
        var identity = await identityDbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userAccountId)
            .Select(user => new { user.UserName, user.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (identity is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(identity.UserName))
        {
            return MaskName(identity.UserName);
        }

        if (!string.IsNullOrWhiteSpace(identity.Email))
        {
            return MaskEmail(identity.Email);
        }

        return "Misafir Kullanıcı";
    }

    private static string MaskName(string userName)
    {
        string trimmed = userName.Trim();

        // If the username looks like an email, mask it as an email.
        if (trimmed.Contains('@', StringComparison.Ordinal))
        {
            return MaskEmail(trimmed);
        }

        if (trimmed.Length <= 2)
        {
            return new string('*', trimmed.Length);
        }

        return string.Concat(trimmed[..1], new string('*', Math.Max(1, trimmed.Length - 2)), trimmed[^1]);
    }

    private static string MaskEmail(string email)
    {
        string trimmed = email.Trim();
        int atIndex = trimmed.IndexOf('@');

        if (atIndex <= 0)
        {
            return new string('*', Math.Min(trimmed.Length, 3));
        }

        string local = trimmed[..atIndex];
        string domain = trimmed[(atIndex + 1)..];

        string maskedLocal = local.Length <= 1
            ? "*"
            : string.Concat(local[..1], new string('*', local.Length - 1));

        int domainDot = domain.LastIndexOf('.');

        string maskedDomain = domainDot > 0
            ? string.Concat(new string('*', domainDot), domain[domainDot..])
            : new string('*', Math.Min(domain.Length, 3));

        return string.Concat(maskedLocal, "@", maskedDomain);
    }
}