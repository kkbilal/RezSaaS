using System.Globalization;
using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessWorkingHoursComposer
{
    private const string Forbidden = "BUSINESS_WORKING_HOURS_FORBIDDEN";
    private const string Unauthorized = "BUSINESS_WORKING_HOURS_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly BranchWorkingHoursManagementService workingHoursService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessWorkingHoursComposer(
        TenantBookingAuthorizationService authorizationService,
        BranchWorkingHoursManagementService workingHoursService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.workingHoursService = workingHoursService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    private async Task<bool> CanManageAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId) return false;
        if (!TryGetUserAccountId(user, out Guid uid)) return false;
        return await authorizationService.CanManageBusinessSettingsAsync(tenantId, uid, ct);
    }

    public async Task<BusinessWorkingHoursResult> ListAsync(ClaimsPrincipal user, Guid branchId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.Forbidden, Forbidden);

        return ToListResult(await workingHoursService.GetForBranchAsync(branchId, ct));
    }

    public async Task<BusinessWorkingHoursResult> UpsertAsync(ClaimsPrincipal user, Guid branchId, string dayOfWeek, BusinessWorkingHoursUpsertRequest request, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.Forbidden, Forbidden);

        if (!Enum.TryParse<DayOfWeek>(dayOfWeek, out var day))
            return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.BadRequest, "INVALID_DAY_OF_WEEK");

        if (!TimeOnly.TryParse(request.OpensAt, out var opensAt) || !TimeOnly.TryParse(request.ClosesAt, out var closesAt))
            return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.BadRequest, "INVALID_TIME_FORMAT");

        return ToResult(await workingHoursService.UpsertAsync(uid, branchId, day, opensAt, closesAt, request.IsClosed, ct));
    }

    public async Task<BusinessWorkingHoursResult> ClearAsync(ClaimsPrincipal user, Guid branchId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.Forbidden, Forbidden);

        return ToListResult(await workingHoursService.ClearBranchAsync(uid, branchId, ct));
    }

    private static BusinessWorkingHoursResult ToResult(BranchWorkingHoursManagementResult result)
    {
        if (result.Succeeded && result.WorkingHours is not null)
            return BusinessWorkingHoursResult.Success(ToResponse(result.WorkingHours));

        return BusinessWorkingHoursResult.Failure(result.ErrorCode switch
        {
            BranchWorkingHoursManagementService.InvalidRequest => BusinessWorkingHoursOutcome.BadRequest,
            _ => BusinessWorkingHoursOutcome.BadRequest,
        }, result.ErrorCode ?? "WORKING_HOURS_FAILED");
    }

    private static BusinessWorkingHoursResult ToListResult(BranchWorkingHoursManagementResult result)
    {
        if (result.Succeeded && result.WorkingHoursList is not null)
            return BusinessWorkingHoursResult.SuccessList(result.WorkingHoursList.Select(ToResponse).ToList());
        return BusinessWorkingHoursResult.Failure(BusinessWorkingHoursOutcome.BadRequest, result.ErrorCode ?? "WORKING_HOURS_FAILED");
    }

    private static BusinessWorkingHoursResponse ToResponse(BranchWorkingHoursView v)
        => new(v.Id, v.DayOfWeek.ToString(), v.OpensAt.ToString("HH:mm", CultureInfo.InvariantCulture), v.ClosesAt.ToString("HH:mm", CultureInfo.InvariantCulture), v.IsClosed);

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid uid)
    {
        string? raw = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out uid);
    }
}
