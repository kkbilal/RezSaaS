using RezSaaS.BuildingBlocks.Tenancy;

namespace RezSaaS.Api.Configuration;

/// <summary>
/// Provides access to the current tenant ID from the request context.
/// </summary>
public sealed class TenantAccessor : ITenantAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return null;

            // Tenant ID is stored in HttpContext.Items by middleware
            if (context.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantId)
                return tenantId;

            return null;
        }
    }
}