using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessResourceEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessResourceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder resources = endpoints
            .MapGroup("/api/business/resources")
            .WithTags("Business Resources")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        resources.MapPost(
            "/{resourceId:guid}/blocks",
            async (
                Guid resourceId,
                [FromBody] BusinessResourceBlockRequest request,
                ClaimsPrincipal user,
                BusinessResourceComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessResourceBlockResult result =
                    await composer.CreateBlockAsync(
                        user,
                        resourceId,
                        request,
                        cancellationToken);

                if (result.Outcome == BusinessAppointmentOutcome.Success)
                {
                    BusinessResourceBlockResponse response = result.Response!;

                    return Results.Created(
                        $"/api/business/resources/{resourceId}/blocks/{response.ResourceBlockId}",
                        response);
                }

                return ToErrorResult(result.Outcome, result.ErrorCode);
            });

        return endpoints;
    }

    private static IResult ToErrorResult(
        BusinessAppointmentOutcome outcome,
        string? errorCode)
    {
        BusinessAppointmentErrorResponse error =
            new(errorCode ?? "BUSINESS_RESOURCE_OPERATION_FAILED");

        return outcome switch
        {
            BusinessAppointmentOutcome.BadRequest => Results.BadRequest(error),
            BusinessAppointmentOutcome.Unauthorized => Results.Unauthorized(),
            BusinessAppointmentOutcome.Forbidden => Results.Forbid(),
            BusinessAppointmentOutcome.NotFound => Results.NotFound(error),
            BusinessAppointmentOutcome.Conflict => Results.Conflict(error),
            _ => Results.UnprocessableEntity(error),
        };
    }
}
