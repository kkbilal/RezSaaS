using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessStaffComposer
{
    private const string Forbidden = "BUSINESS_STAFF_FORBIDDEN";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string Unauthorized = "BUSINESS_STAFF_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly StaffManagementService staffService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessStaffComposer(
        TenantBookingAuthorizationService authorizationService,
        StaffManagementService staffService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.staffService = staffService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessStaffResult> ListByBranchAsync(
        ClaimsPrincipal user,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out _))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Unauthorized, Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Forbidden, Forbidden);
        }

        StaffManagementResult result = await staffService.ListByBranchAsync(branchId, cancellationToken);
        return ToListResult(result);
    }

    public async Task<BusinessStaffResult> GetByIdAsync(
        ClaimsPrincipal user,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out _))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Unauthorized, Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Forbidden, Forbidden);
        }

        StaffManagementResult result = await staffService.GetByIdAsync(staffId, cancellationToken);
        return ToResult(result);
    }

    public async Task<BusinessStaffResult> CreateAsync(
        ClaimsPrincipal user,
        Guid branchId,
        BusinessStaffCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Unauthorized, Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Forbidden, Forbidden);
        }

        StaffManagementResult result = await staffService.CreateAsync(
            new CreateStaffCommand(
                userAccountId,
                branchId,
                request.DisplayName ?? string.Empty,
                request.UserAccountId),
            cancellationToken);

        return ToResult(result);
    }

    public async Task<BusinessStaffResult> UpdateAsync(
        ClaimsPrincipal user,
        Guid branchId,
        Guid staffId,
        BusinessStaffUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Unauthorized, Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Forbidden, Forbidden);
        }

        StaffManagementResult result = await staffService.UpdateAsync(
            new UpdateStaffCommand(userAccountId, branchId, staffId, request.DisplayName ?? string.Empty),
            cancellationToken);

        return ToResult(result);
    }

    public async Task<BusinessStaffResult> ArchiveAsync(
        ClaimsPrincipal user,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Unauthorized, Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessStaffResult.Failure(BusinessStaffOutcome.Forbidden, Forbidden);
        }

        StaffManagementResult result = await staffService.ArchiveAsync(userAccountId, staffId, cancellationToken);
        return ToResult(result);
    }

    private async Task<bool> CanManageAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId) return false;
        if (!TryGetUserAccountId(user, out Guid userAccountId)) return false;
        return await authorizationService.CanManageBusinessSettingsAsync(tenantId, userAccountId, cancellationToken);
    }

    private static BusinessStaffResult ToResult(StaffManagementResult result)
    {
        if (result.Succeeded && result.Staff is not null)
            return BusinessStaffResult.Success(ToResponse(result.Staff));

        BusinessStaffOutcome outcome = result.ErrorCode switch
        {
            StaffManagementService.InvalidRequest => BusinessStaffOutcome.BadRequest,
            StaffManagementService.MissingTenantContext => BusinessStaffOutcome.BadRequest,
            StaffManagementService.BranchNotFound => BusinessStaffOutcome.NotFound,
            StaffManagementService.StaffNotFound => BusinessStaffOutcome.NotFound,
            // 409: personelin gelecekte aktif randevusu var -> arsivlenemez.
            StaffManagementService.StaffHasUpcomingAppointments => BusinessStaffOutcome.Conflict,
            _ => BusinessStaffOutcome.BadRequest,
        };

        return BusinessStaffResult.Failure(outcome, result.ErrorCode ?? "BUSINESS_STAFF_FAILED");
    }

    private static BusinessStaffResult ToListResult(StaffManagementResult result)
    {
        if (result.Succeeded && result.StaffMembers is not null)
            return BusinessStaffResult.SuccessList(result.StaffMembers.Select(ToResponse).ToList());

        return BusinessStaffResult.Failure(BusinessStaffOutcome.BadRequest, result.ErrorCode ?? "BUSINESS_STAFF_FAILED");
    }

    private static BusinessStaffResponse ToResponse(StaffView view)
    {
        return new BusinessStaffResponse(
            view.Id, view.BranchId, view.DisplayName,
            view.UserAccountId, view.Status, view.CreatedAtUtc);
    }

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid userAccountId)
    {
        string? rawUserId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(rawUserId, out userAccountId);
    }
}
