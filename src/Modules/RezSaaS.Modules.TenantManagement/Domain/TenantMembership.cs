namespace RezSaaS.Modules.TenantManagement.Domain;

public sealed class TenantMembership
{
    private TenantMembership()
    {
    }

    private TenantMembership(
        Guid id,
        Guid tenantId,
        Guid userAccountId,
        TenantMembershipRole role,
        DateTimeOffset createdAtUtc,
        Guid? branchId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        }

        if (userAccountId == Guid.Empty)
        {
            throw new ArgumentException("User account id is required.", nameof(userAccountId));
        }

        if (role == TenantMembershipRole.BusinessOwner && branchId is not null)
        {
            throw new ArgumentException("Business owner memberships cannot be branch scoped.", nameof(branchId));
        }

        Id = id;
        BranchId = branchId;
        CreatedAtUtc = createdAtUtc;
        Role = role;
        TenantId = tenantId;
        UserAccountId = userAccountId;
    }

    public Guid? BranchId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public TenantMembershipRole Role { get; private set; }

    public TenantMembershipStatus Status { get; private set; } = TenantMembershipStatus.Active;

    public Tenant? Tenant { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid UserAccountId { get; private set; }

    public static TenantMembership Create(
        Guid tenantId,
        Guid userAccountId,
        TenantMembershipRole role,
        DateTimeOffset createdAtUtc,
        Guid? branchId = null)
    {
        return new TenantMembership(
            Guid.CreateVersion7(),
            tenantId,
            userAccountId,
            role,
            createdAtUtc,
            branchId);
    }

    public void Revoke()
    {
        Status = TenantMembershipStatus.Revoked;
    }

    public void Suspend()
    {
        if (Status == TenantMembershipStatus.Revoked)
        {
            throw new InvalidOperationException("Revoked memberships cannot be suspended.");
        }

        Status = TenantMembershipStatus.Suspended;
    }
}
