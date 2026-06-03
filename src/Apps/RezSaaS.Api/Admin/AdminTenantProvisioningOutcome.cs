namespace RezSaaS.Api.Admin;

public enum AdminTenantProvisioningOutcome
{
    Success,
    Created,
    BadRequest,
    Unauthorized,
    NotFound,
    Conflict,
    Unprocessable,
}
