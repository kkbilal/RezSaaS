using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessAppointmentRequestEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessAppointmentRequestEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder appointmentRequests = endpoints
            .MapGroup("/api/business/appointment-requests")
            .WithTags("Business Appointment Requests")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        appointmentRequests.MapGet(
            "/pending",
            async (
                Guid? branchId,
                int? take,
                ClaimsPrincipal user,
                BusinessAppointmentRequestComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentRequestListResult result =
                    await composer.GetPendingAsync(
                        user,
                        branchId,
                        take,
                        cancellationToken);

                return ToHttpResult(result);
            });

        appointmentRequests.MapGet(
            "/",
            async (
                Guid? branchId,
                string? status,
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                int? take,
                ClaimsPrincipal user,
                BusinessAppointmentRequestComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentRequestListResult result =
                    await composer.GetAsync(
                        user,
                        branchId,
                        status,
                        fromUtc,
                        toUtc,
                        take,
                        cancellationToken);

                return ToHttpResult(result);
            });

        appointmentRequests.MapGet(
            "/{appointmentRequestId:guid}",
            async (
                Guid appointmentRequestId,
                ClaimsPrincipal user,
                BusinessAppointmentRequestComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentRequestListResult result =
                    await composer.GetByIdAsync(
                        user,
                        appointmentRequestId,
                        cancellationToken);

                if (result.Outcome == BusinessAppointmentRequestOutcome.Success)
                {
                    return Results.Ok(result.Requests.Single());
                }

                return ToErrorResult(result.Outcome, result.ErrorCode);
            });

        appointmentRequests.MapPost(
            "/{appointmentRequestId:guid}/approve",
            async (
                Guid appointmentRequestId,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                ClaimsPrincipal user,
                BusinessAppointmentRequestComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentRequestDecisionResult result =
                    await composer.ApproveAsync(
                        user,
                        appointmentRequestId,
                        idempotencyKey,
                        cancellationToken);

                return ToHttpResult(result);
            });

        appointmentRequests.MapPost(
            "/{appointmentRequestId:guid}/decline",
            async (
                Guid appointmentRequestId,
                [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
                ClaimsPrincipal user,
                BusinessAppointmentRequestComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAppointmentRequestDecisionResult result =
                    await composer.DeclineAsync(
                        user,
                        appointmentRequestId,
                        idempotencyKey,
                        cancellationToken);

                return ToHttpResult(result);
            });

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessAppointmentRequestListResult result)
    {
        if (result.Outcome == BusinessAppointmentRequestOutcome.Success)
        {
            return Results.Ok(new BusinessAppointmentRequestListResponse(result.Requests));
        }

        return ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static IResult ToHttpResult(BusinessAppointmentRequestDecisionResult result)
    {
        if (result.Outcome == BusinessAppointmentRequestOutcome.Success)
        {
            return Results.Ok(result.Response);
        }

        return ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static IResult ToErrorResult(
        BusinessAppointmentRequestOutcome outcome,
        string? errorCode)
    {
        BusinessAppointmentRequestErrorResponse error =
            new(errorCode ?? "BUSINESS_APPOINTMENT_REQUEST_FAILED");

        return outcome switch
        {
            BusinessAppointmentRequestOutcome.BadRequest => Results.BadRequest(error),
            BusinessAppointmentRequestOutcome.Unauthorized => Results.Unauthorized(),
            BusinessAppointmentRequestOutcome.Forbidden => Results.Forbid(),
            BusinessAppointmentRequestOutcome.NotFound => Results.NotFound(error),
            BusinessAppointmentRequestOutcome.Conflict => Results.Conflict(error),
            _ => Results.UnprocessableEntity(error),
        };
    }
}
