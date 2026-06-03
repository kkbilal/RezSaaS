namespace RezSaaS.Api.Admin;

public sealed record AdminTenantAccessResult(
    AdminTenantProvisioningOutcome Outcome,
    IReadOnlyCollection<AdminTenantListItemResponse> Tenants,
    AdminTenantDetailResponse? Tenant,
    IReadOnlyCollection<AdminTenantMembershipResponse> Memberships,
    AdminTenantMembershipResponse? Membership,
    string? ErrorCode)
{
    public static AdminTenantAccessResult Success(
        IReadOnlyCollection<AdminTenantListItemResponse> tenants)
    {
        return new AdminTenantAccessResult(
            AdminTenantProvisioningOutcome.Success,
            tenants,
            Tenant: null,
            Memberships: [],
            Membership: null,
            ErrorCode: null);
    }

    public static AdminTenantAccessResult Success(AdminTenantDetailResponse tenant)
    {
        return new AdminTenantAccessResult(
            AdminTenantProvisioningOutcome.Success,
            Tenants: [],
            tenant,
            Memberships: [],
            Membership: null,
            ErrorCode: null);
    }

    public static AdminTenantAccessResult SuccessMemberships(
        IReadOnlyCollection<AdminTenantMembershipResponse> memberships)
    {
        return new AdminTenantAccessResult(
            AdminTenantProvisioningOutcome.Success,
            Tenants: [],
            Tenant: null,
            memberships,
            Membership: null,
            ErrorCode: null);
    }

    public static AdminTenantAccessResult SuccessMembership(AdminTenantMembershipResponse membership)
    {
        return new AdminTenantAccessResult(
            AdminTenantProvisioningOutcome.Success,
            Tenants: [],
            Tenant: null,
            Memberships: [],
            membership,
            ErrorCode: null);
    }

    public static AdminTenantAccessResult Failure(
        AdminTenantProvisioningOutcome outcome,
        string errorCode)
    {
        return new AdminTenantAccessResult(
            outcome,
            Tenants: [],
            Tenant: null,
            Memberships: [],
            Membership: null,
            errorCode);
    }
}
