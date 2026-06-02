namespace RezSaaS.Modules.Organization.Domain;

public sealed class Skill
{
    private Skill()
    {
    }

    private Skill(Guid id, Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", nameof(tenantId));
        }

        Id = id;
        TenantId = tenantId;
        Name = NormalizeRequiredText(name, nameof(name));
        NormalizedName = Name.ToUpperInvariant();
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public static Skill Create(Guid tenantId, string name)
    {
        return new Skill(Guid.CreateVersion7(), tenantId, name);
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
