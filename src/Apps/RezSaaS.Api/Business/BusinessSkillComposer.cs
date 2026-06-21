using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessSkillComposer
{
    private const string Forbidden = "BUSINESS_SKILL_FORBIDDEN";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string Unauthorized = "BUSINESS_SKILL_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly SkillManagementService skillService;
    private readonly StaffSkillService staffSkillService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessSkillComposer(
        TenantBookingAuthorizationService authorizationService,
        SkillManagementService skillService,
        StaffSkillService staffSkillService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.skillService = skillService;
        this.staffSkillService = staffSkillService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessSkillResult> ListAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Unauthorized, Unauthorized);

        if (!await CanManageAsync(user, cancellationToken))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Forbidden, Forbidden);

        SkillManagementResult result = await skillService.ListAsync(cancellationToken);
        return ToListResult(result);
    }

    public async Task<BusinessSkillResult> CreateAsync(
        ClaimsPrincipal user,
        BusinessSkillCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Unauthorized, Unauthorized);

        if (!await CanManageAsync(user, cancellationToken))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Forbidden, Forbidden);

        SkillManagementResult result = await skillService.CreateAsync(
            new CreateSkillCommand(userAccountId, request.Name ?? string.Empty),
            cancellationToken);

        return ToResult(result, StatusCodes.Status201Created);
    }

    public async Task<BusinessSkillResult> DeleteAsync(
        ClaimsPrincipal user,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Unauthorized, Unauthorized);

        if (!await CanManageAsync(user, cancellationToken))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Forbidden, Forbidden);

        SkillManagementResult result = await skillService.DeleteAsync(userAccountId, skillId, cancellationToken);
        return ToResult(result);
    }

    public async Task<BusinessSkillResult> AssignSkillToStaffAsync(
        ClaimsPrincipal user,
        Guid staffMemberId,
        BusinessStaffSkillAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Unauthorized, Unauthorized);

        if (!await CanManageAsync(user, cancellationToken))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Forbidden, Forbidden);

        StaffSkillActionResult result = await staffSkillService.AssignAsync(
            userAccountId, staffMemberId, request.SkillId, cancellationToken);

        return ToStaffSkillResult(result);
    }

    public async Task<BusinessSkillResult> RemoveSkillFromStaffAsync(
        ClaimsPrincipal user,
        Guid staffMemberId,
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Unauthorized, Unauthorized);

        if (!await CanManageAsync(user, cancellationToken))
            return BusinessSkillResult.Failure(BusinessSkillOutcome.Forbidden, Forbidden);

        StaffSkillActionResult result = await staffSkillService.RemoveAsync(
            staffMemberId, skillId, cancellationToken);

        return ToStaffSkillResult(result);
    }

    private async Task<bool> CanManageAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId) return false;
        if (!TryGetUserAccountId(user, out Guid userAccountId)) return false;
        return await authorizationService.CanManageBusinessSettingsAsync(tenantId, userAccountId, cancellationToken);
    }

    private static BusinessSkillResult ToResult(
        SkillManagementResult result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.Succeeded && result.Skill is not null)
        {
            BusinessSkillResponse response = new(result.Skill.Id, result.Skill.Name);
            return BusinessSkillResult.Success(response);
        }

        BusinessSkillOutcome outcome = result.ErrorCode switch
        {
            SkillManagementService.InvalidRequest => BusinessSkillOutcome.BadRequest,
            SkillManagementService.MissingTenantContext => BusinessSkillOutcome.BadRequest,
            SkillManagementService.SkillNotFound => BusinessSkillOutcome.NotFound,
            SkillManagementService.NameConflict => BusinessSkillOutcome.Conflict,
            SkillManagementService.SkillInUse => BusinessSkillOutcome.Conflict,
            _ => BusinessSkillOutcome.BadRequest,
        };

        return BusinessSkillResult.Failure(outcome, result.ErrorCode ?? "BUSINESS_SKILL_FAILED");
    }

    private static BusinessSkillResult ToListResult(SkillManagementResult result)
    {
        if (result.Succeeded && result.Skills is not null)
        {
            return BusinessSkillResult.SuccessList(
                result.Skills.Select(s => new BusinessSkillResponse(s.Id, s.Name)).ToList());
        }

        return BusinessSkillResult.Failure(BusinessSkillOutcome.BadRequest, result.ErrorCode ?? "BUSINESS_SKILL_FAILED");
    }

    private static BusinessSkillResult ToStaffSkillResult(StaffSkillActionResult result)
    {
        if (result.Succeeded)
            return BusinessSkillResult.SuccessList([]);

        BusinessSkillOutcome outcome = result.ErrorCode switch
        {
            StaffSkillService.MissingTenantContext => BusinessSkillOutcome.BadRequest,
            StaffSkillService.StaffNotFound => BusinessSkillOutcome.NotFound,
            StaffSkillService.SkillNotFound => BusinessSkillOutcome.NotFound,
            StaffSkillService.AlreadyAssigned => BusinessSkillOutcome.Conflict,
            StaffSkillService.NotAssigned => BusinessSkillOutcome.NotFound,
            _ => BusinessSkillOutcome.BadRequest,
        };

        return BusinessSkillResult.Failure(outcome, result.ErrorCode ?? "STAFF_SKILL_FAILED");
    }

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid userAccountId)
    {
        string? rawUserId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(rawUserId, out userAccountId);
    }
}
