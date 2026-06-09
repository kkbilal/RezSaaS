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
            })
            .WithName("ListPendingBusinessAppointmentRequests")
            .Produces<BusinessAppointmentRequestListResponse>()
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

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
            })
            .WithName("ListBusinessAppointmentRequests")
            .Produces<BusinessAppointmentRequestListResponse>()
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

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
            })
            .WithName("GetBusinessAppointmentRequest")
            .Produces<BusinessAppointmentRequestResponse>()
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

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
            })
            .WithName("ApproveBusinessAppointmentRequest")
            .Produces<BusinessAppointmentRequestDecisionResponse>()
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

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
            })
            .WithName("DeclineBusinessAppointmentRequest")
            .Produces<BusinessAppointmentRequestDecisionResponse>()
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        appointmentRequests.MapPost(
            "/{appointmentRequestId:guid}/abuse-reports",
            async (
                Guid appointmentRequestId,
                [FromBody] BusinessAbuseReportRequest request,
                ClaimsPrincipal user,
                BusinessAbuseReportComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessAbuseReportAccessResult result =
                    await composer.CreateAsync(
                        appointmentRequestId,
                        request,
                        user,
                        cancellationToken);

                if (result.Outcome == BusinessAbuseReportOutcome.Created)
                {
                    BusinessAbuseReportResponse response = result.Report!;

                    return Results.Created(
                        $"/api/business/appointment-requests/{appointmentRequestId}/abuse-reports/{response.ReportId}",
                        response);
                }

                return result.Outcome == BusinessAbuseReportOutcome.Success
                    ? Results.Ok(result.Report)
                    : ToErrorResult(result.Outcome, result.ErrorCode);
            })
            .WithName("CreateBusinessAppointmentRequestAbuseReport")
            .Produces<BusinessAbuseReportResponse>()
            .Produces<BusinessAbuseReportResponse>(StatusCodes.Status201Created)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<BusinessAppointmentRequestErrorResponse>(StatusCodes.Status422UnprocessableEntity);

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

    private static IResult ToErrorResult(
        BusinessAbuseReportOutcome outcome,
        string? errorCode)
    {
        BusinessAppointmentRequestErrorResponse error =
            new(errorCode ?? "BUSINESS_ABUSE_REPORT_FAILED");

        return outcome switch
        {
            BusinessAbuseReportOutcome.BadRequest => Results.BadRequest(error),
            BusinessAbuseReportOutcome.Unauthorized => Results.Unauthorized(),
            BusinessAbuseReportOutcome.Forbidden => Results.Forbid(),
            BusinessAbuseReportOutcome.NotFound => Results.NotFound(error),
            BusinessAbuseReportOutcome.TooManyRequests => Results.Json(
                error,
                statusCode: StatusCodes.Status429TooManyRequests),
            _ => Results.UnprocessableEntity(error),
        };
    }
}
