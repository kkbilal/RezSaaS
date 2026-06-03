using System.Security.Claims;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.PublicApi;

public static class PublicAppointmentRequestEndpointExtensions
{
    public static IEndpointRouteBuilder MapPublicAppointmentRequestEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder publicBusinesses = endpoints
            .MapGroup("/api/public/businesses")
            .WithTags("Public Appointment Requests")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.AppointmentRequests);

        publicBusinesses.MapPost(
            "/{slug}/appointment-requests",
            async (
                string slug,
                PublicAppointmentRequestCreateRequest request,
                ClaimsPrincipal user,
                PublicAppointmentRequestComposer composer,
                CancellationToken cancellationToken) =>
            {
                PublicAppointmentRequestCreateResult result =
                    await composer.CreateAsync(
                        slug,
                        request,
                        user,
                        cancellationToken);

                return ToHttpResult(slug, result);
            });

        return endpoints;
    }

    private static IResult ToHttpResult(
        string businessSlug,
        PublicAppointmentRequestCreateResult result)
    {
        if (result.Outcome == PublicAppointmentRequestCreateOutcome.Created)
        {
            PublicAppointmentRequestCreateResponse response = result.Response!;
            return Results.Created(
                $"/api/public/businesses/{businessSlug}/appointment-requests/{response.AppointmentRequestId}",
                response);
        }

        PublicAppointmentRequestErrorResponse error = new(result.ErrorCode ?? "PUBLIC_APPOINTMENT_REQUEST_FAILED");

        return result.Outcome switch
        {
            PublicAppointmentRequestCreateOutcome.BadRequest => Results.BadRequest(error),
            PublicAppointmentRequestCreateOutcome.Unauthorized => Results.Unauthorized(),
            PublicAppointmentRequestCreateOutcome.NotFound => Results.NotFound(error),
            PublicAppointmentRequestCreateOutcome.Conflict => Results.Conflict(error),
            PublicAppointmentRequestCreateOutcome.TooManyRequests => Results.StatusCode(StatusCodes.Status429TooManyRequests),
            _ => Results.UnprocessableEntity(error),
        };
    }
}
