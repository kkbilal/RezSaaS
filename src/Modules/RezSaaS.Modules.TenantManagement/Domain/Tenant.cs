namespace RezSaaS.Modules.TenantManagement.Domain;

public sealed class Tenant
{
    private readonly List<TenantMembership> memberships = [];

    private Tenant()
    {
    }

    private Tenant(
        Guid id,
        string slug,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Slug = NormalizeRequiredText(slug, nameof(slug));
        NormalizedSlug = Slug.ToUpperInvariant();
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
        CreatedAtUtc = createdAtUtc;
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public Guid Id { get; private set; }

    public IReadOnlyCollection<TenantMembership> Memberships => memberships.AsReadOnly();

    public string NormalizedSlug { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public TenantStatus Status { get; private set; } = TenantStatus.Active;

    public DateTimeOffset? SuspendedAtUtc { get; private set; }

    public static Tenant Create(
        string slug,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        return new Tenant(Guid.CreateVersion7(), slug, displayName, createdAtUtc);
    }

    public TenantMembership AddMembership(
        Guid userAccountId,
        TenantMembershipRole role,
        DateTimeOffset createdAtUtc,
        Guid? branchId = null)
    {
        TenantMembership membership = TenantMembership.Create(
            Id,
            userAccountId,
            role,
            createdAtUtc,
            branchId);

        memberships.Add(membership);

        return membership;
    }

    public void Close(DateTimeOffset closedAtUtc)
    {
        Status = TenantStatus.Closed;
        ClosedAtUtc = closedAtUtc;
    }

    public void Rename(string displayName)
    {
        DisplayName = NormalizeRequiredText(displayName, nameof(displayName));
    }

    public void Suspend(DateTimeOffset suspendedAtUtc)
    {
        Status = TenantStatus.Suspended;
        SuspendedAtUtc = suspendedAtUtc;
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }
}
