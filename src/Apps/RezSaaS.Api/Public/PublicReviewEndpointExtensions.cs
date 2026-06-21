using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Reviews.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.PublicApi;

public static class PublicReviewEndpointExtensions
{
    public static IEndpointRouteBuilder MapPublicReviewEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/public/businesses/{slug}/reviews")
            .WithTags("Public Reviews");

        group.MapGet(
                "/",
                async (
                    string slug,
                    [FromQuery] int? page,
                    [FromQuery] int? pageSize,
                    PublicReviewComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    PublicReviewSummaryResponse? response =
                        await composer.GetAsync(slug, page ?? 1, pageSize ?? 10, cancellationToken);

                    return response is null
                        ? Results.NotFound()
                        : Results.Ok(response);
                })
            .Produces<PublicReviewSummaryResponse>()
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}