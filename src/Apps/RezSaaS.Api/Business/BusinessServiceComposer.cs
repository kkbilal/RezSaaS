using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Catalog.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessServiceComposer
{
    private const string Forbidden = "BUSINESS_SERVICE_FORBIDDEN";
    private const string Unauthorized = "BUSINESS_SERVICE_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly ServiceManagementService serviceService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessServiceComposer(
        TenantBookingAuthorizationService authorizationService,
        ServiceManagementService serviceService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.serviceService = serviceService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    private async Task<bool> CanManageAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId) return false;
        if (!TryGetUserAccountId(user, out Guid uid)) return false;
        return await authorizationService.CanManageBusinessSettingsAsync(tenantId, uid, ct);
    }

    public async Task<BusinessServiceResult> ListAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Forbidden, Forbidden);

        ServiceManagementResult result = await serviceService.ListAsync(ct);
        return ToListResult(result);
    }

    public async Task<BusinessServiceResult> GetByIdAsync(ClaimsPrincipal user, Guid serviceId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Forbidden, Forbidden);

        return ToResult(await serviceService.GetByIdAsync(serviceId, ct));
    }

    public async Task<BusinessServiceResult> CreateAsync(ClaimsPrincipal user, BusinessServiceCreateRequest request, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Forbidden, Forbidden);

        return ToResult(await serviceService.CreateAsync(new CreateServiceCommand(uid, request.Name, request.CategoryKey), ct));
    }

    public async Task<BusinessServiceResult> UpdateAsync(ClaimsPrincipal user, Guid serviceId, BusinessServiceUpdateRequest request, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Forbidden, Forbidden);

        return ToResult(await serviceService.UpdateAsync(new UpdateServiceCommand(uid, serviceId, request.Name, request.CategoryKey), ct));
    }

    public async Task<BusinessServiceResult> ArchiveAsync(ClaimsPrincipal user, Guid serviceId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessServiceResult.Failure(BusinessServiceOutcome.Forbidden, Forbidden);

        return ToResult(await serviceService.ArchiveAsync(uid, serviceId, ct));
    }

    private static BusinessServiceResult ToResult(ServiceManagementResult result)
    {
        if (result.Succeeded && result.Service is not null)
            return BusinessServiceResult.Success(ToResponse(result.Service));

        return BusinessServiceResult.Failure(result.ErrorCode switch
        {
            ServiceManagementService.InvalidRequest => BusinessServiceOutcome.BadRequest,
            ServiceManagementService.ServiceNotFound => BusinessServiceOutcome.NotFound,
            ServiceManagementService.NameConflict => BusinessServiceOutcome.Conflict,
            _ => BusinessServiceOutcome.BadRequest,
        }, result.ErrorCode ?? "SERVICE_FAILED");
    }

    private static BusinessServiceResult ToListResult(ServiceManagementResult result)
    {
        if (result.Succeeded && result.Services is not null)
            return BusinessServiceResult.SuccessList(result.Services.Select(ToResponse).ToList());
        return BusinessServiceResult.Failure(BusinessServiceOutcome.BadRequest, result.ErrorCode ?? "SERVICE_FAILED");
    }

    private static BusinessServiceResponse ToResponse(ServiceView s)
        => new(s.Id, s.Name, s.CategoryKey, s.Status, s.CreatedAtUtc);

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid uid)
    {
        string? raw = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out uid);
    }
}
