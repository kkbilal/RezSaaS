using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessResourceEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessResourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder resources = endpoints
            .MapGroup("/api/business/branches/{branchId:guid}/resources")
            .WithTags("Business Resources")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        resources.MapGet("/", async (Guid branchId, ClaimsPrincipal user, BusinessResourceComposer composer, CancellationToken ct) =>
            ToListHttpResult(await composer.ListByBranchAsync(user, branchId, ct)))
            .WithName("ListBusinessResources").Produces<List<BusinessResourceResponse>>()
            .Produces(401).Produces(403);

        resources.MapPost("/", async (Guid branchId, [FromBody] BusinessResourceCreateRequest req, ClaimsPrincipal user, BusinessResourceComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.CreateAsync(user, branchId, req, ct), 201))
            .WithName("CreateBusinessResource").Produces<BusinessResourceResponse>(201)
            .Produces<BusinessResourceErrorResponse>(400).Produces(401).Produces(403);

        resources.MapPatch("/{resourceId:guid}", async (Guid branchId, Guid resourceId, [FromBody] BusinessResourceRenameRequest req, ClaimsPrincipal user, BusinessResourceComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.RenameAsync(user, branchId, resourceId, req, ct)))
            .WithName("RenameBusinessResource").Produces<BusinessResourceResponse>()
            .Produces<BusinessResourceErrorResponse>(400).Produces(401).Produces(403).Produces<BusinessResourceErrorResponse>(404);

        resources.MapPost("/{resourceId:guid}/out-of-service", async (Guid branchId, Guid resourceId, ClaimsPrincipal user, BusinessResourceComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.MarkOutOfServiceAsync(user, branchId, resourceId, ct)))
            .WithName("MarkBusinessResourceOutOfService").Produces<BusinessResourceResponse>()
            .Produces(401).Produces(403).Produces<BusinessResourceErrorResponse>(404);

        resources.MapPost("/{resourceId:guid}/restore", async (Guid branchId, Guid resourceId, ClaimsPrincipal user, BusinessResourceComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.RestoreAsync(user, branchId, resourceId, ct)))
            .WithName("RestoreBusinessResource").Produces<BusinessResourceResponse>()
            .Produces(401).Produces(403).Produces<BusinessResourceErrorResponse>(404);

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessResourceResult r, int successCode = 200)
    {
        if (r.Outcome == BusinessResourceOutcome.Success && r.Resource is not null)
            return successCode == 201 ? Results.Created(string.Empty, r.Resource) : Results.Ok(r.Resource);

        var err = new BusinessResourceErrorResponse(r.ErrorCode ?? "RESOURCE_FAILED");
        return r.Outcome switch
        {
            BusinessResourceOutcome.BadRequest => Results.BadRequest(err),
            BusinessResourceOutcome.Unauthorized => Results.Unauthorized(),
            BusinessResourceOutcome.Forbidden => Results.Forbid(),
            BusinessResourceOutcome.NotFound => Results.NotFound(err),
            BusinessResourceOutcome.Conflict => Results.Conflict(err),
            _ => Results.BadRequest(err),
        };
    }

    private static IResult ToListHttpResult(BusinessResourceResult r)
    {
        if (r.Outcome == BusinessResourceOutcome.Success && r.Resources is not null)
            return Results.Ok(r.Resources);

        var err = new BusinessResourceErrorResponse(r.ErrorCode ?? "RESOURCE_FAILED");
        return r.Outcome switch
        {
            BusinessResourceOutcome.BadRequest => Results.BadRequest(err),
            BusinessResourceOutcome.Unauthorized => Results.Unauthorized(),
            BusinessResourceOutcome.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(err),
        };
    }
}
