using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Identity.Configuration;
using RezSaaS.Modules.Identity.Infrastructure.Security;

namespace RezSaaS.Api.Session;

public static class SessionEndpointExtensions
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder session = endpoints
            .MapGroup("/api/session")
            .WithTags("Session")
            .RequireAuthorization()
            .RequireRateLimiting(SessionRateLimitPolicyNames.Bootstrap);

        session
            .MapGet(
                "/bootstrap",
                async (
                    HttpContext httpContext,
                    SessionBootstrapComposer composer,
                    IOptions<StepUpSessionOptions> stepUpOptions,
                    CancellationToken cancellationToken) =>
                {
                    SessionBootstrapResponse? response = await composer.CreateAsync(
                        httpContext.User,
                        httpContext.Request.Cookies[stepUpOptions.Value.CookieName],
                        cancellationToken);

                    return response is null
                        ? Results.Unauthorized()
                        : Results.Ok(response);
                })
            .WithName("GetSessionBootstrap")
            .Produces<SessionBootstrapResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        session
            .MapPost(
                "/step-up",
                async (
                    [FromBody] SessionStepUpRequest request,
                    HttpContext httpContext,
                    StepUpSessionService stepUpSessionService,
                    IOptions<StepUpSessionOptions> stepUpOptions,
                    CancellationToken cancellationToken) =>
                {
                    if (!TryGetUserAccountId(httpContext.User, out Guid userAccountId))
                    {
                        return Results.Unauthorized();
                    }

                    StepUpSessionResult result = await stepUpSessionService.CreateAsync(
                        userAccountId,
                        request.Password,
                        request.TwoFactorCode,
                        request.RecoveryCode,
                        cancellationToken);

                    if (!result.Succeeded)
                    {
                        return ToStepUpError(result.ErrorCode);
                    }

                    StepUpSessionOptions options = stepUpOptions.Value;
                    httpContext.Response.Cookies.Append(
                        options.CookieName,
                        result.Token!,
                        new CookieOptions
                        {
                            Expires = result.Session!.ExpiresAtUtc,
                            HttpOnly = true,
                            IsEssential = true,
                            Path = "/",
                            SameSite = SameSiteMode.Strict,
                            Secure = httpContext.Request.IsHttps,
                        });

                    return Results.Ok(
                        new SessionStepUpCompletedResponse(
                            IsSatisfied: true,
                            result.Session.Method,
                            result.Session.ExpiresAtUtc));
                })
            .WithName("CreateSessionStepUp")
            .Produces<SessionStepUpCompletedResponse>()
            .Produces<SessionStepUpErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<SessionStepUpErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<SessionStepUpErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static IResult ToStepUpError(string? errorCode)
    {
        SessionStepUpErrorResponse error = new(errorCode ?? "STEP_UP_FAILED");

        return error.ErrorCode switch
        {
            "STEP_UP_INVALID_REQUEST" => Results.BadRequest(error),
            "STEP_UP_MFA_REQUIRED" or "STEP_UP_TWO_FACTOR_CODE_REQUIRED" =>
                Results.UnprocessableEntity(error),
            _ => Results.Json(error, statusCode: StatusCodes.Status401Unauthorized),
        };
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
