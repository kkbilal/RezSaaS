using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
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
                    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                    ClaimsPrincipal user,
                    PublicAppointmentRequestComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    PublicAppointmentRequestCreateResult result =
                        await composer.CreateAsync(
                            slug,
                            request,
                            idempotencyKey,
                            user,
                            cancellationToken);

                    return ToHttpResult(slug, result);
                })
            .Produces<PublicAppointmentRequestCreateResponse>(StatusCodes.Status201Created)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status429TooManyRequests);

        publicBusinesses.MapGet(
                "/{slug}/appointment-requests",
                async (
                    string slug,
                    string? status,
                    int? take,
                    ClaimsPrincipal user,
                    PublicAppointmentRequestComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    PublicAppointmentRequestAccessResult result =
                        await composer.GetOwnAsync(
                            slug,
                            status,
                            take,
                            user,
                            cancellationToken);

                    return ToHttpResult(result, listResult: true);
                })
            .Produces<PublicAppointmentRequestListResponse>()
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        publicBusinesses.MapGet(
                "/{slug}/appointment-requests/{appointmentRequestId:guid}",
                async (
                    string slug,
                    Guid appointmentRequestId,
                    ClaimsPrincipal user,
                    PublicAppointmentRequestComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    PublicAppointmentRequestAccessResult result =
                        await composer.GetOwnByIdAsync(
                            slug,
                            appointmentRequestId,
                            user,
                            cancellationToken);

                    return ToHttpResult(result, listResult: false);
                })
            .Produces<PublicAppointmentRequestResponse>()
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        publicBusinesses.MapPost(
                "/{slug}/appointment-requests/{appointmentRequestId:guid}/cancel",
                async (
                    string slug,
                    Guid appointmentRequestId,
                    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                    ClaimsPrincipal user,
                    PublicAppointmentRequestComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    PublicAppointmentRequestAccessResult result =
                        await composer.CancelOwnAsync(
                            slug,
                            appointmentRequestId,
                            idempotencyKey,
                            user,
                            cancellationToken);

                    return ToHttpResult(result, listResult: false);
                })
            .Produces<PublicAppointmentRequestResponse>()
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<PublicAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        // MUSTERI RANDEVU IPTALI -- lansman blokajiydi.
        //
        // Musteri onaylanmis randevusunu HICBIR SEKILDE iptal edemiyordu: talep iptali
        // yalnizca PendingApproval'da calisiyor, isletme tarafindaki iptal ucu ise tenant
        // uyeligi ariyordu. Yani plani degisen musterinin yapacak hicbir seyi yoktu, salonu
        // ARAMAK zorundaydi -- ki bu tam olarak bu urunun cozmeyi vaat ettigi problem.
        publicBusinesses.MapPost(
                "/{slug}/appointments/{appointmentId:guid}/cancel",
                async (
                    string slug,
                    Guid appointmentId,
                    [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                    ClaimsPrincipal user,
                    PublicAppointmentCancellationComposer composer,
                    CancellationToken cancellationToken) =>
                {
                    PublicAppointmentCancellationResult result =
                        await composer.CancelOwnAsync(
                            slug,
                            appointmentId,
                            idempotencyKey,
                            user,
                            cancellationToken);

                    if (result.Outcome == PublicAppointmentRequestAccessOutcome.Success)
                    {
                        return Results.Ok(result.Response);
                    }

                    PublicAppointmentCancellationErrorResponse error =
                        new(result.ErrorCode ?? "APPOINTMENT_CANCEL_INVALID",
                            result.CancellationCutoffHours);

                    return result.Outcome switch
                    {
                        PublicAppointmentRequestAccessOutcome.BadRequest => Results.BadRequest(error),
                        PublicAppointmentRequestAccessOutcome.Unauthorized => Results.Unauthorized(),
                        PublicAppointmentRequestAccessOutcome.NotFound => Results.NotFound(error),
                        PublicAppointmentRequestAccessOutcome.Conflict => Results.Conflict(error),
                        _ => Results.UnprocessableEntity(error),
                    };
                })
            .RequireAuthorization()
            .Produces<PublicAppointmentCancellationResponse>()
            .Produces<PublicAppointmentCancellationErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<PublicAppointmentCancellationErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<PublicAppointmentCancellationErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<PublicAppointmentCancellationErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        return endpoints;
    }

    private static IResult ToHttpResult(
        string businessSlug,
        PublicAppointmentRequestCreateResult result)
    {
        if (result.Outcome == PublicAppointmentRequestCreateOutcome.Created
            || result.Outcome == PublicAppointmentRequestCreateOutcome.Replayed)
        {
            PublicAppointmentRequestCreateResponse response = result.Response!;
            if (result.Outcome == PublicAppointmentRequestCreateOutcome.Replayed)
            {
                return Results.Ok(response);
            }

            return Results.Created(
                $"/api/public/businesses/{businessSlug}/appointment-requests/{response.AppointmentRequestId}",
                response);
        }

        PublicAppointmentRequestErrorResponse error = new(result.ErrorCode ?? "PUBLIC_APPOINTMENT_REQUEST_FAILED");

        return result.Outcome switch
        {
            PublicAppointmentRequestCreateOutcome.BadRequest => Results.BadRequest(error),
            PublicAppointmentRequestCreateOutcome.Unauthorized => Results.Unauthorized(),
            PublicAppointmentRequestCreateOutcome.Forbidden => Results.Forbid(),
            PublicAppointmentRequestCreateOutcome.NotFound => Results.NotFound(error),
            PublicAppointmentRequestCreateOutcome.Conflict => Results.Conflict(error),
            PublicAppointmentRequestCreateOutcome.TooManyRequests => Results.StatusCode(StatusCodes.Status429TooManyRequests),
            _ => Results.UnprocessableEntity(error),
        };
    }

    private static IResult ToHttpResult(
        PublicAppointmentRequestAccessResult result,
        bool listResult)
    {
        if (result.Outcome == PublicAppointmentRequestAccessOutcome.Success)
        {
            return listResult
                ? Results.Ok(new PublicAppointmentRequestListResponse(result.Requests))
                : Results.Ok(result.Request);
        }

        PublicAppointmentRequestErrorResponse error =
            new(result.ErrorCode ?? "PUBLIC_APPOINTMENT_REQUEST_FAILED");

        return result.Outcome switch
        {
            PublicAppointmentRequestAccessOutcome.BadRequest => Results.BadRequest(error),
            PublicAppointmentRequestAccessOutcome.Unauthorized => Results.Unauthorized(),
            PublicAppointmentRequestAccessOutcome.NotFound => Results.NotFound(error),
            PublicAppointmentRequestAccessOutcome.Conflict => Results.Conflict(error),
            _ => Results.UnprocessableEntity(error),
        };
    }
}
