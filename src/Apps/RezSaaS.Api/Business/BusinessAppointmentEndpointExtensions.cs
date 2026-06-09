using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessAppointmentEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessAppointmentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder appointments = endpoints
            .MapGroup("/api/business/appointments")
            .WithTags("Business Appointments")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        appointments.MapGet(
            "/",
            async (
                Guid? branchId,
                string? status,
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                int? take,
                ClaimsPrincipal user,
                BusinessAppointmentComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentListResult result =
                    await composer.GetAsync(
                        user,
                        branchId,
                        status,
                        fromUtc,
                        toUtc,
                        take,
                        cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("ListBusinessAppointments")
            .Produces<BusinessAppointmentListResponse>()
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        appointments.MapGet(
            "/{appointmentId:guid}",
            async (
                Guid appointmentId,
                ClaimsPrincipal user,
                BusinessAppointmentComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentListResult result =
                    await composer.GetByIdAsync(
                        user,
                        appointmentId,
                        cancellationToken);

                if (result.Outcome == BusinessAppointmentOutcome.Success)
                {
                    return Results.Ok(result.Appointments.Single());
                }

                return ToErrorResult(result.Outcome, result.ErrorCode);
            })
            .WithName("GetBusinessAppointment")
            .Produces<BusinessAppointmentResponse>()
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        appointments.MapPost(
            "/{appointmentId:guid}/cancel",
            async (
                Guid appointmentId,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                [FromBody] BusinessAppointmentCancelRequest request,
                ClaimsPrincipal user,
                BusinessAppointmentComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentOperationResult result =
                    await composer.CancelAsync(
                        user,
                        appointmentId,
                        request,
                        idempotencyKey,
                        cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("CancelBusinessAppointment")
            .Produces<BusinessAppointmentOperationResponse>()
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        appointments.MapPost(
            "/{appointmentId:guid}/complete",
            async (
                Guid appointmentId,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                [FromBody] BusinessAppointmentCompleteRequest request,
                ClaimsPrincipal user,
                BusinessAppointmentComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentOperationResult result =
                    await composer.CompleteAsync(
                        user,
                        appointmentId,
                        request,
                        idempotencyKey,
                        cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("CompleteBusinessAppointment")
            .Produces<BusinessAppointmentOperationResponse>()
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        appointments.MapPost(
            "/{appointmentId:guid}/no-show",
            async (
                Guid appointmentId,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                [FromBody] BusinessAppointmentNoShowRequest request,
                ClaimsPrincipal user,
                BusinessAppointmentComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentOperationResult result =
                    await composer.MarkNoShowAsync(
                        user,
                        appointmentId,
                        request,
                        idempotencyKey,
                        cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("MarkBusinessAppointmentNoShow")
            .Produces<BusinessAppointmentOperationResponse>()
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        appointments.MapPost(
            "/{appointmentId:guid}/notes",
            async (
                Guid appointmentId,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                [FromBody] BusinessAppointmentNoteRequest request,
                ClaimsPrincipal user,
                BusinessAppointmentComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentOperationResult result =
                    await composer.UpdateNoteAsync(
                        user,
                        appointmentId,
                        request,
                        idempotencyKey,
                        cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("UpdateBusinessAppointmentNote")
            .Produces<BusinessAppointmentOperationResponse>()
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        appointments.MapPost(
            "/{appointmentId:guid}/rebook",
            async (
                Guid appointmentId,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                [FromBody] BusinessAppointmentRebookRequest request,
                ClaimsPrincipal user,
                BusinessAppointmentComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentOperationResult result =
                    await composer.RebookAsync(
                        user,
                        appointmentId,
                        request,
                        idempotencyKey,
                        cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("RebookBusinessAppointment")
            .Produces<BusinessAppointmentOperationResponse>()
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<BusinessAppointmentErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessAppointmentListResult result)
    {
        if (result.Outcome == BusinessAppointmentOutcome.Success)
        {
            return Results.Ok(new BusinessAppointmentListResponse(result.Appointments));
        }

        return ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static IResult ToHttpResult(BusinessAppointmentOperationResult result)
    {
        if (result.Outcome == BusinessAppointmentOutcome.Success)
        {
            return Results.Ok(result.Response);
        }

        return ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static IResult ToErrorResult(
        BusinessAppointmentOutcome outcome,
        string? errorCode)
    {
        BusinessAppointmentErrorResponse error =
            new(errorCode ?? "BUSINESS_APPOINTMENT_FAILED");

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
