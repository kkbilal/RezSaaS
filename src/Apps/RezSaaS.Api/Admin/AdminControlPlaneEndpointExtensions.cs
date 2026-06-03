using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Security;

namespace RezSaaS.Api.Admin;

public static class AdminControlPlaneEndpointExtensions
{
    public static IEndpointRouteBuilder MapAdminControlPlaneEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder admin = endpoints
            .MapGroup("/api/admin")
            .WithTags("Admin Control Plane");

        admin.MapPost(
                "/bootstrap/platform-admin",
                BootstrapPlatformAdminAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Bootstrap);

        admin.MapPost(
                "/tenants",
                CreateTenantAsync)
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        admin.MapGet(
                "/tenants",
                GetTenantsAsync)
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        admin.MapGet(
                "/tenants/{tenantId:guid}",
                GetTenantByIdAsync)
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        admin.MapGet(
                "/tenants/{tenantId:guid}/memberships",
                GetTenantMembershipsAsync)
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        admin.MapPost(
                "/tenants/{tenantId:guid}/memberships",
                AddTenantMembershipAsync)
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        admin.MapPost(
                "/tenants/{tenantId:guid}/memberships/{membershipId:guid}/suspend",
                SuspendTenantMembershipAsync)
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        admin.MapPost(
                "/tenants/{tenantId:guid}/memberships/{membershipId:guid}/revoke",
                RevokeTenantMembershipAsync)
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        return endpoints;
    }

    private static async Task<IResult> BootstrapPlatformAdminAsync(
        [FromBody] PlatformAdminBootstrapHttpRequest request,
        IPlatformAdminBootstrapService bootstrapService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.BootstrapToken))
        {
            return Results.BadRequest(new AdminControlPlaneErrorResponse("PLATFORM_ADMIN_BOOTSTRAP_INVALID"));
        }

        PlatformAdminBootstrapResult result;

        try
        {
            result = await bootstrapService.BootstrapAsync(
                new PlatformAdminBootstrapRequest(
                    request.Email,
                    request.Password,
                    request.BootstrapToken),
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (result.Succeeded)
        {
            return Results.Ok(new PlatformAdminBootstrapHttpResponse("Bootstrapped"));
        }

        if (result.Reason.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Conflict(
                new AdminControlPlaneErrorResponse("PLATFORM_ADMIN_ALREADY_EXISTS"));
        }

        return Results.BadRequest(new AdminControlPlaneErrorResponse("PLATFORM_ADMIN_BOOTSTRAP_FAILED"));
    }

    private static async Task<IResult> GetTenantsAsync(
        string? search,
        string? status,
        int? take,
        AdminTenantProvisioningComposer composer,
        CancellationToken cancellationToken)
    {
        AdminTenantAccessResult result =
            await composer.GetTenantsAsync(
                search,
                status,
                take,
                cancellationToken);

        return result.Outcome == AdminTenantProvisioningOutcome.Success
            ? Results.Ok(new AdminTenantListResponse(result.Tenants))
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetTenantByIdAsync(
        Guid tenantId,
        AdminTenantProvisioningComposer composer,
        CancellationToken cancellationToken)
    {
        AdminTenantAccessResult result =
            await composer.GetTenantByIdAsync(
                tenantId,
                cancellationToken);

        return result.Outcome == AdminTenantProvisioningOutcome.Success
            ? Results.Ok(result.Tenant)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetTenantMembershipsAsync(
        Guid tenantId,
        AdminTenantProvisioningComposer composer,
        CancellationToken cancellationToken)
    {
        AdminTenantAccessResult result =
            await composer.GetMembershipsAsync(
                tenantId,
                cancellationToken);

        return result.Outcome == AdminTenantProvisioningOutcome.Success
            ? Results.Ok(new AdminTenantMembershipListResponse(result.Memberships))
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> AddTenantMembershipAsync(
        Guid tenantId,
        [FromBody] AdminTenantMembershipCreateRequest request,
        ClaimsPrincipal user,
        AdminTenantProvisioningComposer composer,
        CancellationToken cancellationToken)
    {
        AdminTenantAccessResult result =
            await composer.AddMembershipAsync(
                tenantId,
                request,
                user,
                cancellationToken);

        if (result.Outcome == AdminTenantProvisioningOutcome.Success)
        {
            AdminTenantMembershipResponse response = result.Membership!;

            return Results.Created(
                $"/api/admin/tenants/{tenantId}/memberships/{response.MembershipId}",
                response);
        }

        return ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> RevokeTenantMembershipAsync(
        Guid tenantId,
        Guid membershipId,
        ClaimsPrincipal user,
        AdminTenantProvisioningComposer composer,
        CancellationToken cancellationToken)
    {
        AdminTenantAccessResult result =
            await composer.RevokeMembershipAsync(
                tenantId,
                membershipId,
                user,
                cancellationToken);

        return result.Outcome == AdminTenantProvisioningOutcome.Success
            ? Results.Ok(result.Membership)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> SuspendTenantMembershipAsync(
        Guid tenantId,
        Guid membershipId,
        ClaimsPrincipal user,
        AdminTenantProvisioningComposer composer,
        CancellationToken cancellationToken)
    {
        AdminTenantAccessResult result =
            await composer.SuspendMembershipAsync(
                tenantId,
                membershipId,
                user,
                cancellationToken);

        return result.Outcome == AdminTenantProvisioningOutcome.Success
            ? Results.Ok(result.Membership)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> CreateTenantAsync(
        [FromBody] AdminTenantProvisioningRequest request,
        ClaimsPrincipal user,
        AdminTenantProvisioningComposer composer,
        CancellationToken cancellationToken)
    {
        AdminTenantProvisioningResult result =
            await composer.CreateTenantAsync(
                request,
                user,
                cancellationToken);

        if (result.Outcome == AdminTenantProvisioningOutcome.Created)
        {
            AdminTenantProvisioningResponse response = result.Response!;

            return Results.Created(
                $"/api/admin/tenants/{response.TenantId}",
                response);
        }

        AdminControlPlaneErrorResponse error =
            new(result.ErrorCode ?? "ADMIN_TENANT_PROVISIONING_FAILED");

        return result.Outcome switch
        {
            AdminTenantProvisioningOutcome.BadRequest => Results.BadRequest(error),
            AdminTenantProvisioningOutcome.Unauthorized => Results.Unauthorized(),
            AdminTenantProvisioningOutcome.NotFound => Results.NotFound(error),
            AdminTenantProvisioningOutcome.Conflict => Results.Conflict(error),
            _ => Results.UnprocessableEntity(error),
        };
    }

    private static IResult ToErrorResult(
        AdminTenantProvisioningOutcome outcome,
        string? errorCode)
    {
        AdminControlPlaneErrorResponse error =
            new(errorCode ?? "ADMIN_CONTROL_PLANE_FAILED");

        return outcome switch
        {
            AdminTenantProvisioningOutcome.BadRequest => Results.BadRequest(error),
            AdminTenantProvisioningOutcome.Unauthorized => Results.Unauthorized(),
            AdminTenantProvisioningOutcome.NotFound => Results.NotFound(error),
            AdminTenantProvisioningOutcome.Conflict => Results.Conflict(error),
            _ => Results.UnprocessableEntity(error),
        };
    }
}
