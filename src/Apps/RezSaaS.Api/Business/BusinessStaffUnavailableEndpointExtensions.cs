using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessStaffUnavailableEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessStaffUnavailableEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder unavailable = endpoints
            .MapGroup("/api/business/staff/{staffMemberId:guid}/unavailable")
            .WithTags("Business Staff Unavailable")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        unavailable.MapGet("/", async (Guid staffMemberId, ClaimsPrincipal user, BusinessStaffUnavailableComposer composer, CancellationToken ct) =>
            ToListHttpResult(await composer.ListAsync(user, staffMemberId, ct)))
            .WithName("ListBusinessStaffUnavailable").Produces<List<BusinessStaffUnavailableResponse>>()
            .Produces(401).Produces(403);

        unavailable.MapPost("/", async (Guid staffMemberId, [FromBody] BusinessStaffUnavailableCreateRequest req, ClaimsPrincipal user, BusinessStaffUnavailableComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.CreateAsync(user, staffMemberId, req, ct), 201))
            .WithName("CreateBusinessStaffUnavailable").Produces<BusinessStaffUnavailableResponse>(201)
            .Produces<BusinessStaffUnavailableErrorResponse>(400).Produces(401).Produces(403).Produces<BusinessStaffUnavailableErrorResponse>(409);

        unavailable.MapDelete("/{unavailableId:guid}", async (Guid staffMemberId, Guid unavailableId, ClaimsPrincipal user, BusinessStaffUnavailableComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.DeleteAsync(user, staffMemberId, unavailableId, ct)))
            .WithName("DeleteBusinessStaffUnavailable").Produces<BusinessStaffUnavailableResponse>()
            .Produces(401).Produces(403).Produces<BusinessStaffUnavailableErrorResponse>(404);

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessStaffUnavailableResult r, int successCode = 200)
    {
        if (r.Outcome == BusinessStaffUnavailableOutcome.Success && r.UnavailableTime is not null)
            return successCode == 201 ? Results.Created(string.Empty, r.UnavailableTime) : Results.Ok(r.UnavailableTime);

        var err = new BusinessStaffUnavailableErrorResponse(r.ErrorCode ?? "STAFF_UNAVAILABLE_FAILED");
        return r.Outcome switch
        {
            BusinessStaffUnavailableOutcome.BadRequest => Results.BadRequest(err),
            BusinessStaffUnavailableOutcome.Unauthorized => Results.Unauthorized(),
            BusinessStaffUnavailableOutcome.Forbidden => Results.Forbid(),
            BusinessStaffUnavailableOutcome.NotFound => Results.NotFound(err),
            BusinessStaffUnavailableOutcome.Conflict => Results.Conflict(err),
            _ => Results.BadRequest(err),
        };
    }

    private static IResult ToListHttpResult(BusinessStaffUnavailableResult r)
    {
        if (r.Outcome == BusinessStaffUnavailableOutcome.Success && r.UnavailableTimes is not null)
            return Results.Ok(r.UnavailableTimes);

        var err = new BusinessStaffUnavailableErrorResponse(r.ErrorCode ?? "STAFF_UNAVAILABLE_FAILED");
        return r.Outcome switch
        {
            BusinessStaffUnavailableOutcome.BadRequest => Results.BadRequest(err),
            BusinessStaffUnavailableOutcome.Unauthorized => Results.Unauthorized(),
            BusinessStaffUnavailableOutcome.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(err),
        };
    }
}
