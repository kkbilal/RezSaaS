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
                    CancellationToken cancellationToken) =>
                {
                    SessionBootstrapResponse? response = await composer.CreateAsync(
                        httpContext.User,
                        cancellationToken);

                    return response is null
                        ? Results.Unauthorized()
                        : Results.Ok(response);
                })
            .WithName("GetSessionBootstrap")
            .Produces<SessionBootstrapResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
