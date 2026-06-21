using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessResourceTypeEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessResourceTypeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder types = endpoints
            .MapGroup("/api/business/resource-types")
            .WithTags("Business Resource Types")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        types.MapGet("/", async (ClaimsPrincipal user, BusinessResourceTypeComposer composer, CancellationToken ct) =>
            ToListHttpResult(await composer.ListAsync(user, ct)))
            .WithName("ListBusinessResourceTypes").Produces<List<BusinessResourceTypeResponse>>()
            .Produces(401).Produces(403);

        types.MapPost("/", async ([FromBody] BusinessResourceTypeCreateRequest req, ClaimsPrincipal user, BusinessResourceTypeComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.CreateAsync(user, req, ct), 201))
            .WithName("CreateBusinessResourceType").Produces<BusinessResourceTypeResponse>(201)
            .Produces<BusinessResourceTypeErrorResponse>(400).Produces(401).Produces(403).Produces<BusinessResourceTypeErrorResponse>(409);

        types.MapDelete("/{resourceTypeId:guid}", async (Guid resourceTypeId, ClaimsPrincipal user, BusinessResourceTypeComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.DeleteAsync(user, resourceTypeId, ct)))
            .WithName("DeleteBusinessResourceType").Produces<BusinessResourceTypeResponse>()
            .Produces(401).Produces(403).Produces<BusinessResourceTypeErrorResponse>(404).Produces<BusinessResourceTypeErrorResponse>(409);

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessResourceTypeResult r, int successCode = 200)
    {
        if (r.Outcome == BusinessResourceTypeOutcome.Success && r.ResourceType is not null)
            return successCode == 201 ? Results.Created(string.Empty, r.ResourceType) : Results.Ok(r.ResourceType);

        var err = new BusinessResourceTypeErrorResponse(r.ErrorCode ?? "RESOURCE_TYPE_FAILED");
        return r.Outcome switch
        {
            BusinessResourceTypeOutcome.BadRequest => Results.BadRequest(err),
            BusinessResourceTypeOutcome.Unauthorized => Results.Unauthorized(),
            BusinessResourceTypeOutcome.Forbidden => Results.Forbid(),
            BusinessResourceTypeOutcome.NotFound => Results.NotFound(err),
            BusinessResourceTypeOutcome.Conflict => Results.Conflict(err),
            _ => Results.BadRequest(err),
        };
    }

    private static IResult ToListHttpResult(BusinessResourceTypeResult r)
    {
        if (r.Outcome == BusinessResourceTypeOutcome.Success && r.ResourceTypes is not null)
            return Results.Ok(r.ResourceTypes);

        var err = new BusinessResourceTypeErrorResponse(r.ErrorCode ?? "RESOURCE_TYPE_FAILED");
        return r.Outcome switch
        {
            BusinessResourceTypeOutcome.BadRequest => Results.BadRequest(err),
            BusinessResourceTypeOutcome.Unauthorized => Results.Unauthorized(),
            BusinessResourceTypeOutcome.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(err),
        };
    }
}
