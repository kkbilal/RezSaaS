using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Api.Session;

namespace RezSaaS.Api.Business;

public static class BusinessReviewEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessReviewEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder businessReviews = endpoints
            .MapGroup("/api/business/reviews")
            .WithTags("Business Reviews")
            .RequireAuthorization()
            .RequireRateLimiting(SessionRateLimitPolicyNames.Bootstrap);

        businessReviews
            .MapGet(
                string.Empty,
                async (
                    [FromQuery] string? status,
                    [FromQuery] int page,
                    [FromQuery] int pageSize,
                    ClaimsPrincipal user,
                    BusinessReviewComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    BusinessReviewListResult result = await composer.ListAsync(
                        user,
                        status,
                        page,
                        pageSize,
                        cancellationToken);

                    return result.Outcome switch
                    {
                        BusinessReviewListOutcome.Success => Results.Ok(new
                        {
                            result.TotalCount,
                            result.Page,
                            result.PageSize,
                            Reviews = result.Reviews
                        }),
                        BusinessReviewListOutcome.Unauthorized => Results.Unauthorized(),
                        BusinessReviewListOutcome.BadRequest => Results.BadRequest(new
                        {
                            error_code = result.ErrorCode
                        }),
                        BusinessReviewListOutcome.Forbidden => Results.Forbid(),
                        BusinessReviewListOutcome.NotFound => Results.NotFound(),
                        _ => Results.Problem(
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: "Failed to list reviews"),
                    };
                })
            .WithName("ListBusinessReviews")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        businessReviews
            .MapPost(
                "{reviewId}/moderate",
                async (
                    Guid reviewId,
                    [FromBody] BusinessReviewModerateRequest request,
                    ClaimsPrincipal user,
                    BusinessReviewComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    BusinessReviewModerateResult result = await composer.ModerateAsync(
                        user,
                        reviewId,
                        request,
                        cancellationToken);

                    if (result.Outcome == BusinessReviewModerateOutcome.Success && result.Review is not null)
                    {
                        return Results.Ok(result.Review);
                    }

                    var error = new
                    {
                        error_code = result.ErrorCode ?? "BUSINESS_REVIEW_MODERATE_FAILED"
                    };

                    return result.Outcome switch
                    {
                        BusinessReviewModerateOutcome.Unauthorized => Results.Unauthorized(),
                        BusinessReviewModerateOutcome.BadRequest => Results.BadRequest(error),
                        BusinessReviewModerateOutcome.Forbidden => Results.Forbid(),
                        BusinessReviewModerateOutcome.NotFound => Results.NotFound(),
                        BusinessReviewModerateOutcome.Conflict => Results.Conflict(error),
                        _ => Results.Problem(
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: "Failed to moderate review"),
                    };
                })
            .WithName("ModerateBusinessReview")
            .Produces<BusinessReviewResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return endpoints;
    }
}