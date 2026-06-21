using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Resources.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessResourceTypeComposer
{
    private const string Forbidden = "BUSINESS_RESOURCE_TYPE_FORBIDDEN";
    private const string Unauthorized = "BUSINESS_RESOURCE_TYPE_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly ResourceTypeManagementService resourceTypeService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessResourceTypeComposer(
        TenantBookingAuthorizationService authorizationService,
        ResourceTypeManagementService resourceTypeService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.resourceTypeService = resourceTypeService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    private async Task<bool> CanManageAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId) return false;
        if (!TryGetUserAccountId(user, out Guid uid)) return false;
        return await authorizationService.CanManageBusinessSettingsAsync(tenantId, uid, ct);
    }

    public async Task<BusinessResourceTypeResult> ListAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessResourceTypeResult.Failure(BusinessResourceTypeOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessResourceTypeResult.Failure(BusinessResourceTypeOutcome.Forbidden, Forbidden);

        ResourceTypeManagementResult result = await resourceTypeService.ListAsync(ct);
        return ToListResult(result);
    }

    public async Task<BusinessResourceTypeResult> CreateAsync(ClaimsPrincipal user, BusinessResourceTypeCreateRequest request, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessResourceTypeResult.Failure(BusinessResourceTypeOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessResourceTypeResult.Failure(BusinessResourceTypeOutcome.Forbidden, Forbidden);

        return ToResult(await resourceTypeService.CreateAsync(uid, request.Key, request.DisplayName, ct));
    }

    public async Task<BusinessResourceTypeResult> DeleteAsync(ClaimsPrincipal user, Guid resourceTypeId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessResourceTypeResult.Failure(BusinessResourceTypeOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessResourceTypeResult.Failure(BusinessResourceTypeOutcome.Forbidden, Forbidden);

        return ToResult(await resourceTypeService.DeleteAsync(uid, resourceTypeId, ct));
    }

    private static BusinessResourceTypeResult ToResult(ResourceTypeManagementResult result)
    {
        if (result.Succeeded && result.ResourceType is not null)
            return BusinessResourceTypeResult.Success(ToResponse(result.ResourceType));

        return BusinessResourceTypeResult.Failure(result.ErrorCode switch
        {
            ResourceTypeManagementService.InvalidRequest => BusinessResourceTypeOutcome.BadRequest,
            ResourceTypeManagementService.ResourceTypeNotFound => BusinessResourceTypeOutcome.NotFound,
            ResourceTypeManagementService.KeyConflict => BusinessResourceTypeOutcome.Conflict,
            ResourceTypeManagementService.ResourceTypeInUse => BusinessResourceTypeOutcome.Conflict,
            _ => BusinessResourceTypeOutcome.BadRequest,
        }, result.ErrorCode ?? "RESOURCE_TYPE_FAILED");
    }

    private static BusinessResourceTypeResult ToListResult(ResourceTypeManagementResult result)
    {
        if (result.Succeeded && result.ResourceTypes is not null)
            return BusinessResourceTypeResult.SuccessList(result.ResourceTypes.Select(ToResponse).ToList());
        return BusinessResourceTypeResult.Failure(BusinessResourceTypeOutcome.BadRequest, result.ErrorCode ?? "RESOURCE_TYPE_FAILED");
    }

    private static BusinessResourceTypeResponse ToResponse(ResourceTypeView r)
        => new(r.Id, r.Key, r.DisplayName);

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid uid)
    {
        string? raw = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out uid);
    }
}
