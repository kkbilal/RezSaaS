using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessStaffUnavailableComposer
{
    private const string Forbidden = "BUSINESS_STAFF_UNAVAILABLE_FORBIDDEN";
    private const string Unauthorized = "BUSINESS_STAFF_UNAVAILABLE_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly StaffUnavailableManagementService unavailableService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessStaffUnavailableComposer(
        TenantBookingAuthorizationService authorizationService,
        StaffUnavailableManagementService unavailableService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.unavailableService = unavailableService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    private async Task<bool> CanManageAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId) return false;
        if (!TryGetUserAccountId(user, out Guid uid)) return false;
        return await authorizationService.CanManageBusinessSettingsAsync(tenantId, uid, ct);
    }

    public async Task<BusinessStaffUnavailableResult> ListAsync(ClaimsPrincipal user, Guid staffMemberId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessStaffUnavailableResult.Failure(BusinessStaffUnavailableOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessStaffUnavailableResult.Failure(BusinessStaffUnavailableOutcome.Forbidden, Forbidden);

        return ToListResult(await unavailableService.ListForStaffAsync(staffMemberId, ct));
    }

    public async Task<BusinessStaffUnavailableResult> CreateAsync(ClaimsPrincipal user, Guid staffMemberId, BusinessStaffUnavailableCreateRequest request, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessStaffUnavailableResult.Failure(BusinessStaffUnavailableOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessStaffUnavailableResult.Failure(BusinessStaffUnavailableOutcome.Forbidden, Forbidden);

        return ToResult(await unavailableService.CreateAsync(uid, staffMemberId, request.StartUtc, request.EndUtc, request.Reason, ct));
    }

    public async Task<BusinessStaffUnavailableResult> DeleteAsync(ClaimsPrincipal user, Guid staffMemberId, Guid unavailableId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessStaffUnavailableResult.Failure(BusinessStaffUnavailableOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessStaffUnavailableResult.Failure(BusinessStaffUnavailableOutcome.Forbidden, Forbidden);

        return ToResult(await unavailableService.DeleteAsync(uid, unavailableId, ct));
    }

    private static BusinessStaffUnavailableResult ToResult(StaffUnavailableManagementResult result)
    {
        if (result.Succeeded && result.UnavailableTime is not null)
            return BusinessStaffUnavailableResult.Success(ToResponse(result.UnavailableTime));

        return BusinessStaffUnavailableResult.Failure(result.ErrorCode switch
        {
            StaffUnavailableManagementService.InvalidRequest => BusinessStaffUnavailableOutcome.BadRequest,
            StaffUnavailableManagementService.NotFound => BusinessStaffUnavailableOutcome.NotFound,
            StaffUnavailableManagementService.OverlapConflict => BusinessStaffUnavailableOutcome.Conflict,
            _ => BusinessStaffUnavailableOutcome.BadRequest,
        }, result.ErrorCode ?? "STAFF_UNAVAILABLE_FAILED");
    }

    private static BusinessStaffUnavailableResult ToListResult(StaffUnavailableManagementResult result)
    {
        if (result.Succeeded && result.UnavailableTimes is not null)
            return BusinessStaffUnavailableResult.SuccessList(result.UnavailableTimes.Select(ToResponse).ToList());
        return BusinessStaffUnavailableResult.Failure(BusinessStaffUnavailableOutcome.BadRequest, result.ErrorCode ?? "STAFF_UNAVAILABLE_FAILED");
    }

    private static BusinessStaffUnavailableResponse ToResponse(StaffUnavailableTimeView v)
        => new(v.Id, v.StaffMemberId, v.StartUtc, v.EndUtc, v.Reason);

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid uid)
    {
        string? raw = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out uid);
    }
}
