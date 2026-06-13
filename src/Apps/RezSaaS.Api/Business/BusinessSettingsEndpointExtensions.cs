using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Booking.Application;

namespace RezSaaS.Api.Business;

public static class BusinessSettingsEndpointExtensions
{
    public static IEndpointRouteBuilder MapBusinessSettingsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder settings = endpoints
            .MapGroup("/api/business/settings")
            .WithTags("Business Settings")
            .RequireAuthorization()
            .RequireRateLimiting(BookingRateLimitPolicyNames.BusinessDecisions);

        settings.MapGet(
            "/profile",
            async (
                ClaimsPrincipal user,
                BusinessSettingsComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessSettingsResult result = await composer.GetProfileAsync(
                    user,
                    cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("GetBusinessProfileSettings")
            .Produces<BusinessProfileSettingsResponse>()
            .Produces<BusinessSettingsErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessSettingsErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessSettingsErrorResponse>(StatusCodes.Status409Conflict);

        settings.MapPatch(
            "/profile",
            async (
                [FromBody] BusinessProfileSettingsRequest request,
                ClaimsPrincipal user,
                BusinessSettingsComposer composer,
                CancellationToken cancellationToken) =>
            {
                BusinessSettingsResult result = await composer.UpdateProfileAsync(
                    user,
                    request,
                    cancellationToken);

                return ToHttpResult(result);
            })
            .WithName("UpdateBusinessProfileSettings")
            .Produces<BusinessProfileSettingsResponse>()
            .Produces<BusinessSettingsErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<BusinessSettingsErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<BusinessSettingsErrorResponse>(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static IResult ToHttpResult(BusinessSettingsResult result)
    {
        if (result.Outcome == BusinessSettingsOutcome.Success)
        {
            return Results.Ok(result.Profile);
        }

        BusinessSettingsErrorResponse error =
            new(result.ErrorCode ?? "BUSINESS_SETTINGS_FAILED");

        return result.Outcome switch
        {
            BusinessSettingsOutcome.BadRequest => Results.BadRequest(error),
            BusinessSettingsOutcome.Unauthorized => Results.Unauthorized(),
            BusinessSettingsOutcome.Forbidden => Results.Forbid(),
            BusinessSettingsOutcome.NotFound => Results.NotFound(error),
            BusinessSettingsOutcome.Conflict => Results.Conflict(error),
            _ => Results.BadRequest(error),
        };
    }
}
