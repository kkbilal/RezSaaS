using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Security;

namespace RezSaaS.Api.Admin;

public static class AdminAbuseControlPlaneEndpointExtensions
{
    public static IEndpointRouteBuilder MapAdminAbuseControlPlaneEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder abuse = endpoints
            .MapGroup("/api/admin/abuse")
            .WithTags("Admin Abuse Control Plane")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        abuse.MapGet("/events", GetEventsAsync);
        abuse.MapGet("/users/{userAccountId:guid}", GetUserOverviewAsync);
        abuse.MapPost("/users/{userAccountId:guid}/sanctions", ApplySanctionAsync);
        abuse.MapPost(
            "/users/{userAccountId:guid}/sanctions/{sanctionId:guid}/revoke",
            RevokeSanctionAsync);

        return endpoints;
    }

    private static async Task<IResult> ApplySanctionAsync(
        Guid userAccountId,
        [FromBody] AdminApplyUserSanctionRequest request,
        ClaimsPrincipal user,
        AdminAbuseControlPlaneComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseAccessResult result =
            await composer.ApplySanctionAsync(
                userAccountId,
                request,
                user,
                cancellationToken);

        if (result.Outcome == AdminAbuseOutcome.Created)
        {
            AdminUserSanctionResponse response = result.Sanction!;

            return Results.Created(
                $"/api/admin/abuse/users/{userAccountId}/sanctions/{response.SanctionId}",
                response);
        }

        return ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetEventsAsync(
        Guid? userAccountId,
        Guid? tenantId,
        string? severity,
        int? take,
        AdminAbuseControlPlaneComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseAccessResult result =
            await composer.GetEventsAsync(
                userAccountId,
                tenantId,
                severity,
                take,
                cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(new AdminAbuseEventListResponse(result.Events))
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> RevokeSanctionAsync(
        Guid userAccountId,
        Guid sanctionId,
        [FromBody] AdminRevokeUserSanctionRequest request,
        ClaimsPrincipal user,
        AdminAbuseControlPlaneComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseAccessResult result =
            await composer.RevokeSanctionAsync(
                userAccountId,
                sanctionId,
                request,
                user,
                cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(result.Sanction)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetUserOverviewAsync(
        Guid userAccountId,
        int? take,
        AdminAbuseControlPlaneComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseAccessResult result =
            await composer.GetUserOverviewAsync(
                userAccountId,
                take,
                cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(result.Overview)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static IResult ToErrorResult(
        AdminAbuseOutcome outcome,
        string? errorCode)
    {
        AdminControlPlaneErrorResponse error =
            new(errorCode ?? "ADMIN_ABUSE_FAILED");

        return outcome switch
        {
            AdminAbuseOutcome.BadRequest => Results.BadRequest(error),
            AdminAbuseOutcome.Unauthorized => Results.Unauthorized(),
            AdminAbuseOutcome.NotFound => Results.NotFound(error),
            AdminAbuseOutcome.Conflict => Results.Conflict(error),
            _ => Results.UnprocessableEntity(error),
        };
    }
}
