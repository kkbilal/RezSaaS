using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Identity.Configuration;

namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed class PlatformStepUpAuthorizationHandler
    : AuthorizationHandler<PlatformStepUpRequirement>
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly StepUpSessionOptions options;
    private readonly StepUpSessionService stepUpSessionService;

    public PlatformStepUpAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IOptions<StepUpSessionOptions> options,
        StepUpSessionService stepUpSessionService)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.options = options.Value;
        this.stepUpSessionService = stepUpSessionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformStepUpRequirement requirement)
    {
        if (context.User.Claims.Any(claim =>
            string.Equals(claim.Type, "amr", StringComparison.Ordinal)
            && string.Equals(claim.Value, requirement.Method, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return;
        }

        HttpContext? httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null
            || !TryGetUserAccountId(context.User, out Guid userAccountId)
            || !httpContext.Request.Cookies.TryGetValue(options.CookieName, out string? token))
        {
            return;
        }

        StepUpSessionView? session = await stepUpSessionService.ValidateAsync(
            userAccountId,
            token,
            requirement.Method,
            httpContext.RequestAborted);

        if (session is not null)
        {
            context.Succeed(requirement);
        }
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal principal,
        out Guid userAccountId)
    {
        string? rawUserAccountId = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserAccountId, out userAccountId);
    }
}
