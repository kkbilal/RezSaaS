using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessBranchEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessBranchEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder branches = endpoints
            .MapGroup("/api/business/branches")
            .WithTags("Business Branches")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        branches.MapGet(
            "/",
            async (
                ClaimsPrincipal user,
                BusinessBranchComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessBranchResult result = await composer.ListAsync(
                    user,
                    cancellationToken);

                return ToListHttpResult(result);
            })
            .WithName("ListBusinessBranches")
            .Produces<List<BusinessBranchResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        branches.MapGet(
            "/{branchId:guid}",
            async (
                Guid branchId,
                ClaimsPrincipal user,
                BusinessBranchComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessBranchResult result = await composer.GetByIdAsync(
                    user,
                    branchId,
                    cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("GetBusinessBranch")
            .Produces<BusinessBranchResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessBranchErrorResponse>(StatusCodes.Status404NotFound);

        branches.MapPost(
            "/",
            async (
                [FromBody] BusinessBranchCreateRequest request,
                ClaimsPrincipal user,
                BusinessBranchComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessBranchResult result = await composer.CreateAsync(
                    user,
                    request,
                    cancellationToken);

                return ToHttpResult(result, StatusCodes.Status201Created);
            })
            .WithName("CreateBusinessBranch")
            .Produces<BusinessBranchResponse>(StatusCodes.Status201Created)
            .Produces<BusinessBranchErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessBranchErrorResponse>(StatusCodes.Status409Conflict);

        branches.MapPatch(
            "/{branchId:guid}",
            async (
                Guid branchId,
                [FromBody] BusinessBranchUpdateRequest request,
                ClaimsPrincipal user,
                BusinessBranchComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessBranchResult result = await composer.UpdateAsync(
                    user,
                    branchId,
                    request,
                    cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("UpdateBusinessBranch")
            .Produces<BusinessBranchResponse>()
            .Produces<BusinessBranchErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessBranchErrorResponse>(StatusCodes.Status404NotFound);

        branches.MapPatch(
            "/{branchId:guid}/slot-settings",
            async (
                Guid branchId,
                [FromBody] BusinessBranchSlotSettingsRequest request,
                ClaimsPrincipal user,
                BusinessBranchComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessBranchResult result = await composer.UpdateSlotSettingsAsync(
                    user,
                    branchId,
                    request,
                    cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("UpdateBusinessBranchSlotSettings")
            .Produces<BusinessBranchResponse>()
            .Produces<BusinessBranchErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessBranchErrorResponse>(StatusCodes.Status404NotFound);

        branches.MapPost(
            "/{branchId:guid}/archive",
            async (
                Guid branchId,
                ClaimsPrincipal user,
                BusinessBranchComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessBranchResult result = await composer.ArchiveAsync(
                    user,
                    branchId,
                    cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("ArchiveBusinessBranch")
            .Produces<BusinessBranchResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessBranchErrorResponse>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static IResult ToHttpResult(
        BusinessBranchResult result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.Outcome == BusinessBranchOutcome.Success && result.Branch is not null)
        {
            return successStatusCode == StatusCodes.Status201Created
                ? Results.Created($"/api/business/branches/{result.Branch.Id}", result.Branch)
                : Results.Ok(result.Branch);
        }

        BusinessBranchErrorResponse error =
            new(result.ErrorCode ?? "BUSINESS_BRANCH_FAILED");

        return result.Outcome switch
        {
            BusinessBranchOutcome.BadRequest => Results.BadRequest(error),
            BusinessBranchOutcome.Unauthorized => Results.Unauthorized(),
            BusinessBranchOutcome.Forbidden => Results.Forbid(),
            BusinessBranchOutcome.NotFound => Results.NotFound(error),
            BusinessBranchOutcome.Conflict => Results.Conflict(error),
            _ => Results.BadRequest(error),
        };
    }

    private static IResult ToListHttpResult(BusinessBranchResult result)
    {
        if (result.Outcome == BusinessBranchOutcome.Success && result.Branches is not null)
        {
            return Results.Ok(result.Branches);
        }

        BusinessBranchErrorResponse error =
            new(result.ErrorCode ?? "BUSINESS_BRANCH_FAILED");

        return result.Outcome switch
        {
            BusinessBranchOutcome.BadRequest => Results.BadRequest(error),
            BusinessBranchOutcome.Unauthorized => Results.Unauthorized(),
            BusinessBranchOutcome.Forbidden => Results.Forbid(),
            BusinessBranchOutcome.NotFound => Results.NotFound(error),
            _ => Results.BadRequest(error),
        };
    }
}
