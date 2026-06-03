namespace RezSaaS.Api.Admin;

public sealed record AdminTenantProvisioningResult(
    AdminTenantProvisioningOutcome Outcome,
    AdminTenantProvisioningResponse? Response,
    string? ErrorCode)
{
    public static AdminTenantProvisioningResult Created(AdminTenantProvisioningResponse response)
    {
        return new AdminTenantProvisioningResult(
            AdminTenantProvisioningOutcome.Created,
            response,
            ErrorCode: null);
    }

    public static AdminTenantProvisioningResult Failure(
        AdminTenantProvisioningOutcome outcome,
        string errorCode)
    {
        return new AdminTenantProvisioningResult(
            outcome,
            Response: null,
            errorCode);
    }
}
