using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessServiceEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessServiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder services = endpoints
            .MapGroup("/api/business/services")
            .WithTags("Business Services")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        services.MapGet("/", async (ClaimsPrincipal user, BusinessServiceComposer composer, CancellationToken ct) =>
            ToListHttpResult(await composer.ListAsync(user, ct)))
            .WithName("ListBusinessServices").Produces<List<BusinessServiceResponse>>()
            .Produces(401).Produces(403);

        services.MapGet("/{serviceId:guid}", async (Guid serviceId, ClaimsPrincipal user, BusinessServiceComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.GetByIdAsync(user, serviceId, ct)))
            .WithName("GetBusinessService").Produces<BusinessServiceResponse>()
            .Produces(401).Produces(403).Produces<BusinessServiceErrorResponse>(404);

        services.MapPost("/", async ([FromBody] BusinessServiceCreateRequest req, ClaimsPrincipal user, BusinessServiceComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.CreateAsync(user, req, ct), 201))
            .WithName("CreateBusinessService").Produces<BusinessServiceResponse>(201)
            .Produces<BusinessServiceErrorResponse>(400).Produces(401).Produces(403).Produces<BusinessServiceErrorResponse>(409);

        services.MapPatch("/{serviceId:guid}", async (Guid serviceId, [FromBody] BusinessServiceUpdateRequest req, ClaimsPrincipal user, BusinessServiceComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.UpdateAsync(user, serviceId, req, ct)))
            .WithName("UpdateBusinessService").Produces<BusinessServiceResponse>()
            .Produces<BusinessServiceErrorResponse>(400).Produces(401).Produces(403).Produces<BusinessServiceErrorResponse>(404).Produces<BusinessServiceErrorResponse>(409);

        services.MapPost("/{serviceId:guid}/archive", async (Guid serviceId, ClaimsPrincipal user, BusinessServiceComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.ArchiveAsync(user, serviceId, ct)))
            .WithName("ArchiveBusinessService").Produces<BusinessServiceResponse>()
            .Produces(401).Produces(403).Produces<BusinessServiceErrorResponse>(404).Produces<BusinessServiceErrorResponse>(409);

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessServiceResult r, int successCode = 200)
    {
        if (r.Outcome == BusinessServiceOutcome.Success && r.Service is not null)
            return successCode == 201 ? Results.Created(string.Empty, r.Service) : Results.Ok(r.Service);

        var err = new BusinessServiceErrorResponse(r.ErrorCode ?? "SERVICE_FAILED");
        return r.Outcome switch
        {
            BusinessServiceOutcome.BadRequest => Results.BadRequest(err),
            BusinessServiceOutcome.Unauthorized => Results.Unauthorized(),
            BusinessServiceOutcome.Forbidden => Results.Forbid(),
            BusinessServiceOutcome.NotFound => Results.NotFound(err),
            BusinessServiceOutcome.Conflict => Results.Conflict(err),
            _ => Results.BadRequest(err),
        };
    }

    private static IResult ToListHttpResult(BusinessServiceResult r)
    {
        if (r.Outcome == BusinessServiceOutcome.Success && r.Services is not null)
            return Results.Ok(r.Services);

        var err = new BusinessServiceErrorResponse(r.ErrorCode ?? "SERVICE_FAILED");
        return r.Outcome switch
        {
            BusinessServiceOutcome.BadRequest => Results.BadRequest(err),
            BusinessServiceOutcome.Unauthorized => Results.Unauthorized(),
            BusinessServiceOutcome.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(err),
        };
    }
}
