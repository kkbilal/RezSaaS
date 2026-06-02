namespace RezSaaS.Modules.Catalog.Domain;

public sealed class ServiceRequiredSkill
{
    private ServiceRequiredSkill()
    {
    }

    private ServiceRequiredSkill(Guid id, Guid tenantId, Guid serviceVariantId, Guid skillId)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(serviceVariantId, nameof(serviceVariantId));
        RequireNonEmpty(skillId, nameof(skillId));

        Id = id;
        TenantId = tenantId;
        ServiceVariantId = serviceVariantId;
        SkillId = skillId;
    }

    public Guid Id { get; private set; }

    public Guid ServiceVariantId { get; private set; }

    public Guid SkillId { get; private set; }

    public Guid TenantId { get; private set; }

    public static ServiceRequiredSkill Create(Guid tenantId, Guid serviceVariantId, Guid skillId)
    {
        return new ServiceRequiredSkill(Guid.CreateVersion7(), tenantId, serviceVariantId, skillId);
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
