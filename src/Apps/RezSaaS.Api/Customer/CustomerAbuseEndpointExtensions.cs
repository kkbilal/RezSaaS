using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Api.Admin;

namespace RezSaaS.Api.Customer;

public static class CustomerAbuseEndpointExtensions
{
    public static IEndpointRouteBuilder MapCustomerAbuseEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder customerAbuse = endpoints
            .MapGroup("/api/customer/abuse")
            .WithTags("Customer Abuse Appeals")
            .RequireAuthorization()
            .RequireRateLimiting(CustomerAbuseRateLimitPolicyNames.Actions);

        customerAbuse.MapGet("/overview", GetOverviewAsync)
            .WithName("GetCustomerAbuseOverview")
            .Produces<CustomerAbuseOverviewResponse>()
            .ProducesCustomerAbuseErrors();

        customerAbuse.MapGet("/appeals/{appealId:guid}", GetAppealAsync)
            .WithName("GetCustomerAbuseAppeal")
            .Produces<CustomerAbuseAppealResponse>()
            .ProducesCustomerAbuseErrors();

        customerAbuse.MapPost("/appeals", CreateAppealAsync)
            .WithName("CreateCustomerAbuseAppeal")
            .Produces<CustomerAbuseAppealResponse>(StatusCodes.Status200OK)
            .Produces<CustomerAbuseAppealResponse>(StatusCodes.Status201Created)
            .ProducesCustomerAbuseErrors();

        return endpoints;
    }

    private static async Task<IResult> CreateAppealAsync(
        [FromBody] CustomerCreateAbuseAppealRequest request,
        ClaimsPrincipal user,
        CustomerAbuseComposer composer,
        CancellationToken cancellationToken)
    {
        CustomerAbuseAccessResult result =
            await composer.CreateAppealAsync(request, user, cancellationToken);

        if (result.Outcome == CustomerAbuseOutcome.Created)
        {
            CustomerAbuseAppealResponse response = result.Appeal!;

            return Results.Created(
                $"/api/customer/abuse/appeals/{response.AppealId}",
                response);
        }

        return result.Outcome == CustomerAbuseOutcome.Success
            ? Results.Ok(result.Appeal)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetOverviewAsync(
        ClaimsPrincipal user,
        CustomerAbuseComposer composer,
        CancellationToken cancellationToken)
    {
        CustomerAbuseAccessResult result =
            await composer.GetOverviewAsync(user, cancellationToken);

        return result.Outcome == CustomerAbuseOutcome.Success
            ? Results.Ok(result.Overview)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetAppealAsync(
        Guid appealId,
        ClaimsPrincipal user,
        CustomerAbuseComposer composer,
        CancellationToken cancellationToken)
    {
        CustomerAbuseAccessResult result =
            await composer.GetAppealAsync(appealId, user, cancellationToken);

        return result.Outcome == CustomerAbuseOutcome.Success
            ? Results.Ok(result.Appeal)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static IResult ToErrorResult(
        CustomerAbuseOutcome outcome,
        string? errorCode)
    {
        AdminControlPlaneErrorResponse error =
            new(errorCode ?? "CUSTOMER_ABUSE_FAILED");

        return outcome switch
        {
            CustomerAbuseOutcome.BadRequest => Results.BadRequest(error),
            CustomerAbuseOutcome.Unauthorized => Results.Unauthorized(),
            CustomerAbuseOutcome.NotFound => Results.NotFound(error),
            CustomerAbuseOutcome.Conflict => Results.Conflict(error),
            _ => Results.UnprocessableEntity(error),
        };
    }

    private static RouteHandlerBuilder ProducesCustomerAbuseErrors(
        this RouteHandlerBuilder builder)
    {
        return builder
            .Produces<AdminControlPlaneErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<AdminControlPlaneErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<AdminControlPlaneErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<AdminControlPlaneErrorResponse>(StatusCodes.Status422UnprocessableEntity)
            .Produces(StatusCodes.Status429TooManyRequests);
    }
}
