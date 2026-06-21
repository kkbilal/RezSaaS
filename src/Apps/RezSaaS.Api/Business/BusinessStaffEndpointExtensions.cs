using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessStaffEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessStaffEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder staff = endpoints
            .MapGroup("/api/business/branches/{branchId:guid}/staff")
            .WithTags("Business Staff")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        staff.MapGet(
            "/",
            async (
                Guid branchId,
                ClaimsPrincipal user,
                BusinessStaffComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessStaffResult result = await composer.ListByBranchAsync(
                    user, branchId, cancellationToken);
                return ToListHttpResult(result);
            })
            .WithName("ListBusinessStaff")
            .Produces<List<BusinessStaffResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        staff.MapGet(
            "/{staffId:guid}",
            async (
                Guid branchId,
                Guid staffId,
                ClaimsPrincipal user,
                BusinessStaffComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessStaffResult result = await composer.GetByIdAsync(
                    user, staffId, cancellationToken);
                return ToHttpResult(result);
            })
            .WithName("GetBusinessStaff")
            .Produces<BusinessStaffResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessStaffErrorResponse>(StatusCodes.Status404NotFound);

        staff.MapPost(
            "/",
            async (
                Guid branchId,
                [FromBody] BusinessStaffCreateRequest request,
                ClaimsPrincipal user,
                BusinessStaffComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessStaffResult result = await composer.CreateAsync(
                    user, branchId, request, cancellationToken);
                return ToHttpResult(result, StatusCodes.Status201Created);
            })
            .WithName("CreateBusinessStaff")
            .Produces<BusinessStaffResponse>(StatusCodes.Status201Created)
            .Produces<BusinessStaffErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        staff.MapPatch(
            "/{staffId:guid}",
            async (
                Guid branchId,
                Guid staffId,
                [FromBody] BusinessStaffUpdateRequest request,
                ClaimsPrincipal user,
                BusinessStaffComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessStaffResult result = await composer.UpdateAsync(
                    user, branchId, staffId, request, cancellationToken);
                return ToHttpResult(result);
            })
            .WithName("UpdateBusinessStaff")
            .Produces<BusinessStaffResponse>()
            .Produces<BusinessStaffErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessStaffErrorResponse>(StatusCodes.Status404NotFound);

        staff.MapPost(
            "/{staffId:guid}/archive",
            async (
                Guid branchId,
                Guid staffId,
                ClaimsPrincipal user,
                BusinessStaffComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessStaffResult result = await composer.ArchiveAsync(
                    user, staffId, cancellationToken);
                return ToHttpResult(result);
            })
            .WithName("ArchiveBusinessStaff")
            .Produces<BusinessStaffResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessStaffErrorResponse>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static IResult ToHttpResult(
        BusinessStaffResult result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.Outcome == BusinessStaffOutcome.Success && result.Staff is not null)
        {
            return successStatusCode == StatusCodes.Status201Created
                ? Results.Created(string.Empty, result.Staff)
                : Results.Ok(result.Staff);
        }

        BusinessStaffErrorResponse error = new(result.ErrorCode ?? "BUSINESS_STAFF_FAILED");

        return result.Outcome switch
        {
            BusinessStaffOutcome.BadRequest => Results.BadRequest(error),
            BusinessStaffOutcome.Unauthorized => Results.Unauthorized(),
            BusinessStaffOutcome.Forbidden => Results.Forbid(),
            BusinessStaffOutcome.NotFound => Results.NotFound(error),
            BusinessStaffOutcome.Conflict => Results.Conflict(error),
            _ => Results.BadRequest(error),
        };
    }

    private static IResult ToListHttpResult(BusinessStaffResult result)
    {
        if (result.Outcome == BusinessStaffOutcome.Success && result.StaffMembers is not null)
            return Results.Ok(result.StaffMembers);

        BusinessStaffErrorResponse error = new(result.ErrorCode ?? "BUSINESS_STAFF_FAILED");
        return result.Outcome switch
        {
            BusinessStaffOutcome.BadRequest => Results.BadRequest(error),
            BusinessStaffOutcome.Unauthorized => Results.Unauthorized(),
            BusinessStaffOutcome.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(error),
        };
    }
}
