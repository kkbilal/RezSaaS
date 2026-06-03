using RezSaaS.Modules.Organization.Application;

namespace RezSaaS.Api.PublicApi;

public static class PublicBusinessProfileEndpointExtensions
{
    public static IEndpointRouteBuilder MapPublicBusinessProfileEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder publicBusinesses = endpoints
            .MapGroup("/api/public/businesses")
            .WithTags("Public Businesses")
            .RequireRateLimiting(OrganizationRateLimitPolicyNames.PublicDiscovery);

        publicBusinesses.MapGet(
            "/{slug}/profile",
            async (
                string slug,
                PublicBusinessProfileComposer composer,
                CancellationToken cancellationToken) =>
            {
                PublicBusinessProfileResponse? profile =
                    await composer.GetProfileAsync(slug, cancellationToken);

                return profile is null
                    ? Results.NotFound()
                    : Results.Ok(profile);
            });

        return endpoints;
    }
}
