namespace RezSaaS.Modules.Organization.Domain;

public sealed class StaffSkill
{
    private StaffSkill()
    {
    }

    private StaffSkill(Guid id, Guid tenantId, Guid staffMemberId, Guid skillId)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(staffMemberId, nameof(staffMemberId));
        RequireNonEmpty(skillId, nameof(skillId));

        Id = id;
        TenantId = tenantId;
        StaffMemberId = staffMemberId;
        SkillId = skillId;
    }

    public Guid Id { get; private set; }

    public Guid SkillId { get; private set; }

    public Guid StaffMemberId { get; private set; }

    public Guid TenantId { get; private set; }

    public static StaffSkill Create(Guid tenantId, Guid staffMemberId, Guid skillId)
    {
        return new StaffSkill(Guid.CreateVersion7(), tenantId, staffMemberId, skillId);
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
