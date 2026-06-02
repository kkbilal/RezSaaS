using RezSaaS.BuildingBlocks.Tenancy;

namespace RezSaaS.Api.Configuration;

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor tenantContextAccessor)
    {
        if (context.Request.Headers.TryGetValue(TenantContextHeaders.TenantId, out var tenantValues))
        {
            string? rawTenantId = tenantValues.FirstOrDefault();

            if (!Guid.TryParse(rawTenantId, out Guid tenantId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid tenant context header.");
                return;
            }

            tenantContextAccessor.TenantId = tenantId;
        }

        await next(context);
    }
}
