using System.Security.Claims;
using RezSaaS.Modules.Identity.Application;

namespace RezSaaS.Api.Configuration;

public sealed class ActiveUserAccountMiddleware
{
    private readonly RequestDelegate next;

    public ActiveUserAccountMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        UserAccountExistenceService userAccountExistenceService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            string? rawUserAccountId = context.User.FindFirstValue("sub")
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(rawUserAccountId, out Guid userAccountId)
                || !await userAccountExistenceService.ExistsActiveAsync(
                    userAccountId,
                    context.RequestAborted))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }
}
