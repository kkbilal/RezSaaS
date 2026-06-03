namespace RezSaaS.Api.Admin;

public enum AdminTenantProvisioningOutcome
{
    Created,
    BadRequest,
    Unauthorized,
    NotFound,
    Conflict,
    Unprocessable,
}
