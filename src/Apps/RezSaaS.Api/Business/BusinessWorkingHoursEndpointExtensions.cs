using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessWorkingHoursEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessWorkingHoursEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder hours = endpoints
            .MapGroup("/api/business/branches/{branchId:guid}/working-hours")
            .WithTags("Business Working Hours")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        hours.MapGet("/", async (Guid branchId, ClaimsPrincipal user, BusinessWorkingHoursComposer composer, CancellationToken ct) =>
            ToListHttpResult(await composer.ListAsync(user, branchId, ct)))
            .WithName("ListBusinessWorkingHours").Produces<List<BusinessWorkingHoursResponse>>()
            .Produces(401).Produces(403);

        hours.MapPut("/{dayOfWeek}", async (Guid branchId, string dayOfWeek, [FromBody] BusinessWorkingHoursUpsertRequest req, ClaimsPrincipal user, BusinessWorkingHoursComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.UpsertAsync(user, branchId, dayOfWeek, req, ct)))
            .WithName("UpsertBusinessWorkingHours").Produces<BusinessWorkingHoursResponse>()
            .Produces<BusinessWorkingHoursErrorResponse>(400).Produces(401).Produces(403);

        hours.MapDelete("/", async (Guid branchId, ClaimsPrincipal user, BusinessWorkingHoursComposer composer, CancellationToken ct) =>
            ToListHttpResult(await composer.ClearAsync(user, branchId, ct)))
            .WithName("ClearBusinessWorkingHours").Produces<List<BusinessWorkingHoursResponse>>()
            .Produces(401).Produces(403).Produces(404);

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessWorkingHoursResult r, int successCode = 200)
    {
        if (r.Outcome == BusinessWorkingHoursOutcome.Success && r.WorkingHours is not null)
            return successCode == 201 ? Results.Created(string.Empty, r.WorkingHours) : Results.Ok(r.WorkingHours);

        var err = new BusinessWorkingHoursErrorResponse(r.ErrorCode ?? "WORKING_HOURS_FAILED");
        return r.Outcome switch
        {
            BusinessWorkingHoursOutcome.BadRequest => Results.BadRequest(err),
            BusinessWorkingHoursOutcome.Unauthorized => Results.Unauthorized(),
            BusinessWorkingHoursOutcome.Forbidden => Results.Forbid(),
            BusinessWorkingHoursOutcome.NotFound => Results.NotFound(err),
            _ => Results.BadRequest(err),
        };
    }

    private static IResult ToListHttpResult(BusinessWorkingHoursResult r)
    {
        if (r.Outcome == BusinessWorkingHoursOutcome.Success && r.WorkingHoursList is not null)
            return Results.Ok(r.WorkingHoursList);

        var err = new BusinessWorkingHoursErrorResponse(r.ErrorCode ?? "WORKING_HOURS_FAILED");
        return r.Outcome switch
        {
            BusinessWorkingHoursOutcome.BadRequest => Results.BadRequest(err),
            BusinessWorkingHoursOutcome.Unauthorized => Results.Unauthorized(),
            BusinessWorkingHoursOutcome.Forbidden => Results.Forbid(),
            BusinessWorkingHoursOutcome.NotFound => Results.NotFound(err),
            _ => Results.BadRequest(err),
        };
    }
}
