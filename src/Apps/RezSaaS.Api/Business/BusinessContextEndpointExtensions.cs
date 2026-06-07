using RezSaaS.Api.Session;

namespace RezSaaS.Api.Business;

public static class BusinessContextEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessContextEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder business = endpoints
            .MapGroup("/api/business")
            .WithTags("Business Context")
            .RequireAuthorization()
            .RequireRateLimiting(SessionRateLimitPolicyNames.Bootstrap);

        business
            .MapGet(
                "/context",
                async (
                    HttpContext httpContext,
                    BusinessContextComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    BusinessContextResponse? response = await composer.CreateAsync(
                        httpContext.User,
                        cancellationToken);

                    return response is null
                        ? Results.Unauthorized()
                        : Results.Ok(response);
                })
            .WithName("GetBusinessContext")
            .Produces<BusinessContextResponse>()
            .Produces(StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}
