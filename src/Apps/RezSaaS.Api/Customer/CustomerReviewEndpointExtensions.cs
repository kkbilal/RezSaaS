using System.Security.Claims;
using RezSaaS.Api.Session;
using RezSaaS.Modules.Reviews.Application;

namespace RezSaaS.Api.Customer;

public static class CustomerReviewEndpointExtensions
{
    public static IEndpointRouteBuilder MapCustomerReviewEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder customerReviews = endpoints
            .MapGroup("/api/customer/reviews")
            .WithTags("Customer Reviews")
            .RequireAuthorization()
            .RequireRateLimiting(SessionRateLimitPolicyNames.Bootstrap);

        customerReviews
            .MapPost(
                string.Empty,
                async (
                    CustomerCreateReviewRequest body,
                    ClaimsPrincipal user,
                    CustomerCreateReviewComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    CustomerCreateReviewResult result =
                        await composer.CreateAsync(user, body, cancellationToken);

                    if (result.Outcome == CustomerCreateReviewOutcome.Success && result.Review is not null)
                    {
                        ReviewView review = result.Review;
                        return Results.Created(
                            $"/api/customer/reviews/{review.Id}",
                            new CustomerCreateReviewResponse(
                                review.Id,
                                review.BusinessId,
                                review.BranchId,
                                review.AppointmentId,
                                review.Rating,
                                review.Comment,
                                review.Status,
                                review.CreatedAtUtc,
                                review.ModeratedAtUtc,
                                review.ServiceNames));
                    }

                    CustomerCreateReviewErrorResponse error =
                        new(result.ErrorCode ?? "CUSTOMER_REVIEW_FAILED");

                    return result.Outcome switch
                    {
                        CustomerCreateReviewOutcome.Unauthorized => Results.Unauthorized(),
                        CustomerCreateReviewOutcome.BadRequest => Results.BadRequest(error),
                        CustomerCreateReviewOutcome.NotFound => Results.NotFound(),
                        CustomerCreateReviewOutcome.Conflict => Results.Conflict(error),
                        _ => Results.Problem(
                            statusCode: StatusCodes.Status500InternalServerError,
                            title: "Customer review failed"),
                    };
                })
            .WithName("CreateCustomerReview")
            .Produces<CustomerCreateReviewResponse>(StatusCodes.Status201Created)
            .Produces<CustomerCreateReviewErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<CustomerCreateReviewErrorResponse>(StatusCodes.Status409Conflict);

        return endpoints;
    }
}

public sealed record CustomerCreateReviewErrorResponse(string ErrorCode);