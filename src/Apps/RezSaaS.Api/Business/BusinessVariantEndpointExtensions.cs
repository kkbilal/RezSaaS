using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessVariantEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessVariantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var variants = endpoints
            .MapGroup("/api/business/services/{serviceId:guid}/variants")
            .WithTags("Business Service Variants")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        variants.MapGet("/", async (Guid serviceId, ClaimsPrincipal user, BusinessVariantComposer composer, CancellationToken ct) =>
            ToListHttpResult(await composer.ListByServiceAsync(user, serviceId, ct)))
            .WithName("ListServiceVariants").Produces<List<BusinessVariantResponse>>()
            .Produces(401).Produces(403);

        variants.MapGet("/{variantId:guid}", async (Guid serviceId, Guid variantId, ClaimsPrincipal user, BusinessVariantComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.GetByIdAsync(user, variantId, ct)))
            .WithName("GetServiceVariant").Produces<BusinessVariantResponse>()
            .Produces(401).Produces(403).Produces<BusinessVariantErrorResponse>(404);

        variants.MapPost("/", async (Guid serviceId, [FromBody] BusinessVariantCreateRequest req, ClaimsPrincipal user, BusinessVariantComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.CreateAsync(user, serviceId, req, ct), 201))
            .WithName("CreateServiceVariant").Produces<BusinessVariantResponse>(201)
            .Produces<BusinessVariantErrorResponse>(400).Produces(401).Produces(403).Produces<BusinessVariantErrorResponse>(404).Produces<BusinessVariantErrorResponse>(409);

        variants.MapPatch("/{variantId:guid}", async (Guid serviceId, Guid variantId, [FromBody] BusinessVariantUpdateRequest req, ClaimsPrincipal user, BusinessVariantComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.UpdateAsync(user, serviceId, variantId, req, ct)))
            .WithName("UpdateServiceVariant").Produces<BusinessVariantResponse>()
            .Produces<BusinessVariantErrorResponse>(400).Produces(401).Produces(403)
            .Produces<BusinessVariantErrorResponse>(404).Produces<BusinessVariantErrorResponse>(409);

        variants.MapDelete("/{variantId:guid}", async (Guid serviceId, Guid variantId, ClaimsPrincipal user, BusinessVariantComposer composer, CancellationToken ct) =>
            ToHttpResult(await composer.DeleteAsync(user, variantId, ct)))
            .WithName("DeleteServiceVariant").Produces(200)
            .Produces(401).Produces(403).Produces<BusinessVariantErrorResponse>(404);

        var requiredSkills = endpoints
            .MapGroup("/api/business/services/{serviceId:guid}/variants/{variantId:guid}/required-skills")
            .WithTags("Business Service Required Skills")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        requiredSkills.MapPost("/{skillId:guid}", async (Guid serviceId, Guid variantId, Guid skillId, ClaimsPrincipal user, BusinessVariantComposer composer, CancellationToken ct) =>
            ToSkillHttpResult(await composer.AssignRequiredSkillAsync(user, variantId, skillId, ct)))
            .WithName("AssignRequiredSkill").Produces(200)
            .Produces(400).Produces(401).Produces(403).Produces(404).Produces(409);

        requiredSkills.MapDelete("/{skillId:guid}", async (Guid serviceId, Guid variantId, Guid skillId, ClaimsPrincipal user, BusinessVariantComposer composer, CancellationToken ct) =>
            ToSkillHttpResult(await composer.RemoveRequiredSkillAsync(user, variantId, skillId, ct)))
            .WithName("RemoveRequiredSkill").Produces(200)
            .Produces(401).Produces(403).Produces(404);

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessVariantResult r, int successCode = 200)
    {
        if (r.Outcome == BusinessVariantOutcome.Success && r.Variant is not null)
            return successCode == 201 ? Results.Created(string.Empty, r.Variant) : Results.Ok(r.Variant);

        var err = new BusinessVariantErrorResponse(r.ErrorCode ?? "VARIANT_FAILED");
        return r.Outcome switch
        {
            BusinessVariantOutcome.BadRequest => Results.BadRequest(err),
            BusinessVariantOutcome.Unauthorized => Results.Unauthorized(),
            BusinessVariantOutcome.Forbidden => Results.Forbid(),
            BusinessVariantOutcome.NotFound => Results.NotFound(err),
            BusinessVariantOutcome.Conflict => Results.Conflict(err),
            _ => Results.BadRequest(err),
        };
    }

    private static IResult ToListHttpResult(BusinessVariantResult r)
    {
        if (r.Outcome == BusinessVariantOutcome.Success && r.Variants is not null)
            return Results.Ok(r.Variants);

        var err = new BusinessVariantErrorResponse(r.ErrorCode ?? "VARIANT_FAILED");
        return r.Outcome switch
        {
            BusinessVariantOutcome.BadRequest => Results.BadRequest(err),
            BusinessVariantOutcome.Unauthorized => Results.Unauthorized(),
            BusinessVariantOutcome.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(err),
        };
    }

    private static IResult ToSkillHttpResult(BusinessVariantResult r)
    {
        if (r.Outcome == BusinessVariantOutcome.Success) return Results.Ok();

        var err = new BusinessVariantErrorResponse(r.ErrorCode ?? "REQUIRED_SKILL_FAILED");
        return r.Outcome switch
        {
            BusinessVariantOutcome.BadRequest => Results.BadRequest(err),
            BusinessVariantOutcome.Unauthorized => Results.Unauthorized(),
            BusinessVariantOutcome.Forbidden => Results.Forbid(),
            BusinessVariantOutcome.NotFound => Results.NotFound(err),
            BusinessVariantOutcome.Conflict => Results.Conflict(err),
            _ => Results.BadRequest(err),
        };
    }
}
