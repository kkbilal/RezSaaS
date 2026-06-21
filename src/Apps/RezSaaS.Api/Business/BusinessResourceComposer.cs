using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Resources.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessResourceComposer
{
    private const string Forbidden = "BUSINESS_RESOURCE_FORBIDDEN";
    private const string Unauthorized = "BUSINESS_RESOURCE_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly ResourceManagementService resourceService;
    private readonly ResourceOperationalBlockService blockService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessResourceComposer(
        TenantBookingAuthorizationService authorizationService,
        ResourceManagementService resourceService,
        ResourceOperationalBlockService blockService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.resourceService = resourceService;
        this.blockService = blockService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    private async Task<bool> CanManageAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId) return false;
        if (!TryGetUserAccountId(user, out Guid uid)) return false;
        return await authorizationService.CanManageBusinessSettingsAsync(tenantId, uid, ct);
    }

    public async Task<BusinessResourceResult> ListByBranchAsync(ClaimsPrincipal user, Guid branchId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Forbidden, Forbidden);

        ResourceManagementResult result = await resourceService.ListByBranchAsync(branchId, ct);
        return ToListResult(result);
    }

    public async Task<BusinessResourceResult> CreateAsync(ClaimsPrincipal user, Guid branchId, BusinessResourceCreateRequest request, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Forbidden, Forbidden);

        return ToResult(await resourceService.CreateAsync(uid, branchId, request.ResourceTypeId, request.DisplayName, ct));
    }

    public async Task<BusinessResourceResult> RenameAsync(ClaimsPrincipal user, Guid branchId, Guid resourceId, BusinessResourceRenameRequest request, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Forbidden, Forbidden);

        return ToResult(await resourceService.RenameAsync(uid, resourceId, request.DisplayName, ct));
    }

    public async Task<BusinessResourceResult> MarkOutOfServiceAsync(ClaimsPrincipal user, Guid branchId, Guid resourceId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Forbidden, Forbidden);

        return ToResult(await resourceService.MarkOutOfServiceAsync(uid, resourceId, ct));
    }

    public async Task<BusinessResourceResult> RestoreAsync(ClaimsPrincipal user, Guid branchId, Guid resourceId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessResourceResult.Failure(BusinessResourceOutcome.Forbidden, Forbidden);

        return ToResult(await resourceService.RestoreAsync(uid, resourceId, ct));
    }

    private static BusinessResourceResult ToResult(ResourceManagementResult result)
    {
        if (result.Succeeded && result.Resource is not null)
            return BusinessResourceResult.Success(ToResponse(result.Resource));

        return BusinessResourceResult.Failure(result.ErrorCode switch
        {
            ResourceManagementService.InvalidRequest => BusinessResourceOutcome.BadRequest,
            ResourceManagementService.ResourceNotFound => BusinessResourceOutcome.NotFound,
            ResourceManagementService.ResourceTypeNotFound => BusinessResourceOutcome.NotFound,
            _ => BusinessResourceOutcome.BadRequest,
        }, result.ErrorCode ?? "RESOURCE_FAILED");
    }

    private static BusinessResourceResult ToListResult(ResourceManagementResult result)
    {
        if (result.Succeeded && result.Resources is not null)
            return BusinessResourceResult.SuccessList(result.Resources.Select(ToResponse).ToList());
        return BusinessResourceResult.Failure(BusinessResourceOutcome.BadRequest, result.ErrorCode ?? "RESOURCE_FAILED");
    }

    private static BusinessResourceResponse ToResponse(ResourceView r)
        => new(r.Id, r.ResourceTypeId, r.DisplayName, r.Status);

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid uid)
    {
        string? raw = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out uid);
    }
}
