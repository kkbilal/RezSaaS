using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessBranchComposer
{
    private const string Forbidden = "BUSINESS_BRANCH_FORBIDDEN";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string Unauthorized = "BUSINESS_BRANCH_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly BranchManagementService branchService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessBranchComposer(
        TenantBookingAuthorizationService authorizationService,
        BranchManagementService branchService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.branchService = branchService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessBranchResult> ListAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out _))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Unauthorized,
                Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Forbidden,
                Forbidden);
        }

        BranchManagementResult result = await branchService.ListAsync(cancellationToken);

        return ToListResult(result);
    }

    public async Task<BusinessBranchResult> GetByIdAsync(
        ClaimsPrincipal user,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out _))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Unauthorized,
                Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Forbidden,
                Forbidden);
        }

        BranchManagementResult result = await branchService.GetByIdAsync(
            branchId,
            cancellationToken);

        return ToResult(result);
    }

    public async Task<BusinessBranchResult> CreateAsync(
        ClaimsPrincipal user,
        BusinessBranchCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Unauthorized,
                Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Forbidden,
                Forbidden);
        }

        BranchManagementResult result = await branchService.CreateAsync(
            new CreateBranchCommand(
                userAccountId,
                request.Slug ?? string.Empty,
                request.DisplayName ?? string.Empty,
                request.TimeZoneId ?? string.Empty,
                request.City ?? string.Empty,
                request.District ?? string.Empty,
                request.AddressLine ?? string.Empty),
            cancellationToken);

        return ToResult(result);
    }

    public async Task<BusinessBranchResult> UpdateAsync(
        ClaimsPrincipal user,
        Guid branchId,
        BusinessBranchUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Unauthorized,
                Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Forbidden,
                Forbidden);
        }

        BranchManagementResult result = await branchService.UpdateAsync(
            new UpdateBranchCommand(
                userAccountId,
                branchId,
                request.DisplayName ?? string.Empty,
                request.City ?? string.Empty,
                request.District ?? string.Empty,
                request.AddressLine ?? string.Empty),
            cancellationToken);

        return ToResult(result);
    }

    public async Task<BusinessBranchResult> UpdateSlotSettingsAsync(
        ClaimsPrincipal user,
        Guid branchId,
        BusinessBranchSlotSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Unauthorized,
                Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Forbidden,
                Forbidden);
        }

        BranchManagementResult result = await branchService.UpdateSlotSettingsAsync(
            new UpdateBranchSlotSettingsCommand(
                userAccountId,
                branchId,
                request.SlotIntervalMinutes,
                request.MaxPublicSlots),
            cancellationToken);

        return ToResult(result);
    }

    public async Task<BusinessBranchResult> ArchiveAsync(
        ClaimsPrincipal user,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Unauthorized,
                Unauthorized);
        }

        if (!await CanManageAsync(user, cancellationToken))
        {
            return BusinessBranchResult.Failure(
                BusinessBranchOutcome.Forbidden,
                Forbidden);
        }

        BranchManagementResult result = await branchService.ArchiveAsync(
            userAccountId,
            branchId,
            cancellationToken);

        return ToResult(result);
    }

    private async Task<bool> CanManageAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return false;
        }

        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return false;
        }

        return await authorizationService.CanManageBusinessSettingsAsync(
            tenantId,
            userAccountId,
            cancellationToken);
    }

    private static BusinessBranchResult ToResult(BranchManagementResult result)
    {
        if (result.Succeeded && result.Branch is not null)
        {
            return BusinessBranchResult.Success(ToResponse(result.Branch));
        }

        BusinessBranchOutcome outcome = result.ErrorCode switch
        {
            BranchManagementService.InvalidRequest => BusinessBranchOutcome.BadRequest,
            BranchManagementService.MissingTenantContext => BusinessBranchOutcome.BadRequest,
            BranchManagementService.BusinessNotFound => BusinessBranchOutcome.NotFound,
            BranchManagementService.BranchNotFound => BusinessBranchOutcome.NotFound,
            BranchManagementService.SlugConflict => BusinessBranchOutcome.Conflict,
            BranchManagementService.BranchHasStaff => BusinessBranchOutcome.Conflict,
            _ => BusinessBranchOutcome.BadRequest,
        };

        return BusinessBranchResult.Failure(
            outcome,
            result.ErrorCode ?? "BUSINESS_BRANCH_FAILED");
    }

    private static BusinessBranchResult ToListResult(BranchManagementResult result)
    {
        if (result.Succeeded && result.Branches is not null)
        {
            return BusinessBranchResult.SuccessList(
                result.Branches.Select(ToResponse).ToList());
        }

        BusinessBranchOutcome outcome = result.ErrorCode switch
        {
            BranchManagementService.MissingTenantContext => BusinessBranchOutcome.BadRequest,
            _ => BusinessBranchOutcome.BadRequest,
        };

        return BusinessBranchResult.Failure(
            outcome,
            result.ErrorCode ?? "BUSINESS_BRANCH_FAILED");
    }

    private static BusinessBranchResponse ToResponse(BranchView view)
    {
        return new BusinessBranchResponse(
            view.Id,
            view.Slug,
            view.DisplayName,
            view.TimeZoneId,
            view.City,
            view.District,
            view.AddressLine,
            view.SlotIntervalMinutes,
            view.MaxPublicSlots,
            view.CreatedAtUtc);
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(rawUserId, out userAccountId);
    }
}
