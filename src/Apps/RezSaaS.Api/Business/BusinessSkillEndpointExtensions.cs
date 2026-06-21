using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessSkillEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessSkillEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder skills = endpoints
            .MapGroup("/api/business/skills")
            .WithTags("Business Skills")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        skills.MapGet(
            "/",
            async (
                ClaimsPrincipal user,
                BusinessSkillComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessSkillResult result = await composer.ListAsync(user, cancellationToken);
                return ToListHttpResult(result);
            })
            .WithName("ListBusinessSkills")
            .Produces<List<BusinessSkillResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        skills.MapPost(
            "/",
            async (
                [FromBody] BusinessSkillCreateRequest request,
                ClaimsPrincipal user,
                BusinessSkillComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessSkillResult result = await composer.CreateAsync(user, request, cancellationToken);
                return ToHttpResult(result, StatusCodes.Status201Created);
            })
            .WithName("CreateBusinessSkill")
            .Produces<BusinessSkillResponse>(StatusCodes.Status201Created)
            .Produces<BusinessSkillErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessSkillErrorResponse>(StatusCodes.Status409Conflict);

        skills.MapDelete(
            "/{skillId:guid}",
            async (
                Guid skillId,
                ClaimsPrincipal user,
                BusinessSkillComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessSkillResult result = await composer.DeleteAsync(user, skillId, cancellationToken);
                return ToHttpResult(result);
            })
            .WithName("DeleteBusinessSkill")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessSkillErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessSkillErrorResponse>(StatusCodes.Status409Conflict);

        RouteGroupBuilder staffSkills = endpoints
            .MapGroup("/api/business/staff/{staffMemberId:guid}/skills")
            .WithTags("Business Skills")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        staffSkills.MapPost(
            "/",
            async (
                Guid staffMemberId,
                [FromBody] BusinessStaffSkillAssignRequest request,
                ClaimsPrincipal user,
                BusinessSkillComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessSkillResult result = await composer.AssignSkillToStaffAsync(
                    user, staffMemberId, request, cancellationToken);
                return ToStaffSkillHttpResult(result);
            })
            .WithName("AssignSkillToStaff")
            .Produces(StatusCodes.Status200OK)
            .Produces<BusinessSkillErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessSkillErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessSkillErrorResponse>(StatusCodes.Status409Conflict);

        staffSkills.MapDelete(
            "/{skillId:guid}",
            async (
                Guid staffMemberId,
                Guid skillId,
                ClaimsPrincipal user,
                BusinessSkillComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessSkillResult result = await composer.RemoveSkillFromStaffAsync(
                    user, staffMemberId, skillId, cancellationToken);
                return ToStaffSkillHttpResult(result);
            })
            .WithName("RemoveSkillFromStaff")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessSkillErrorResponse>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static IResult ToHttpResult(
        BusinessSkillResult result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.Outcome == BusinessSkillOutcome.Success && result.Skill is not null)
        {
            return successStatusCode == StatusCodes.Status201Created
                ? Results.Created(string.Empty, result.Skill)
                : Results.Ok(result.Skill);
        }

        BusinessSkillErrorResponse error = new(result.ErrorCode ?? "BUSINESS_SKILL_FAILED");

        return result.Outcome switch
        {
            BusinessSkillOutcome.BadRequest => Results.BadRequest(error),
            BusinessSkillOutcome.Unauthorized => Results.Unauthorized(),
            BusinessSkillOutcome.Forbidden => Results.Forbid(),
            BusinessSkillOutcome.NotFound => Results.NotFound(error),
            BusinessSkillOutcome.Conflict => Results.Conflict(error),
            _ => Results.BadRequest(error),
        };
    }

    private static IResult ToListHttpResult(BusinessSkillResult result)
    {
        if (result.Outcome == BusinessSkillOutcome.Success && result.Skills is not null)
            return Results.Ok(result.Skills);

        BusinessSkillErrorResponse error = new(result.ErrorCode ?? "BUSINESS_SKILL_FAILED");
        return result.Outcome switch
        {
            BusinessSkillOutcome.BadRequest => Results.BadRequest(error),
            BusinessSkillOutcome.Unauthorized => Results.Unauthorized(),
            BusinessSkillOutcome.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(error),
        };
    }

    private static IResult ToStaffSkillHttpResult(BusinessSkillResult result)
    {
        if (result.Outcome == BusinessSkillOutcome.Success)
            return Results.Ok();

        BusinessSkillErrorResponse error = new(result.ErrorCode ?? "STAFF_SKILL_FAILED");

        return result.Outcome switch
        {
            BusinessSkillOutcome.BadRequest => Results.BadRequest(error),
            BusinessSkillOutcome.Unauthorized => Results.Unauthorized(),
            BusinessSkillOutcome.Forbidden => Results.Forbid(),
            BusinessSkillOutcome.NotFound => Results.NotFound(error),
            BusinessSkillOutcome.Conflict => Results.Conflict(error),
            _ => Results.BadRequest(error),
        };
    }
}
