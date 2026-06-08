using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Resources.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessResourceComposer
{
    private const string Forbidden = "BUSINESS_RESOURCE_FORBIDDEN";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "RESOURCE_NOT_FOUND";
    private const string Unauthorized = "BUSINESS_RESOURCE_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly ResourceOperationalBlockService resourceBlockService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessResourceComposer(
        TenantBookingAuthorizationService authorizationService,
        ResourceOperationalBlockService resourceBlockService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.resourceBlockService = resourceBlockService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessResourceBlockResult> CreateBlockAsync(
        ClaimsPrincipal user,
        Guid resourceId,
        BusinessResourceBlockRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessResourceBlockResult.Failure(
                BusinessAppointmentOutcome.Unauthorized,
                Unauthorized);
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessResourceBlockResult.Failure(
                BusinessAppointmentOutcome.BadRequest,
                MissingTenantContext);
        }

        Guid? branchId = await resourceBlockService.GetResourceBranchIdAsync(
            resourceId,
            cancellationToken);

        if (branchId is null)
        {
            return BusinessResourceBlockResult.Failure(
                BusinessAppointmentOutcome.NotFound,
                NotFound);
        }

        if (!await authorizationService.CanManageAppointmentRequestsAsync(
            tenantId,
            userAccountId,
            branchId,
            cancellationToken))
        {
            return BusinessResourceBlockResult.Failure(
                BusinessAppointmentOutcome.Forbidden,
                Forbidden);
        }

        ResourceBlockCommandResult result = await resourceBlockService.CreateBlockAsync(
            resourceId,
            userAccountId,
            request.StartUtc,
            request.EndUtc,
            request.Reason,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BusinessResourceBlockResult.Failure(
                result.ErrorCode == "RESOURCE_BLOCK_CONFLICT"
                    ? BusinessAppointmentOutcome.Conflict
                    : BusinessAppointmentOutcome.BadRequest,
                result.ErrorCode ?? "RESOURCE_BLOCK_FAILED");
        }

        ResourceBlockView block = result.Block!;

        return BusinessResourceBlockResult.Success(
            new BusinessResourceBlockResponse(
                block.Id,
                block.ResourceId,
                block.BranchId,
                block.StartUtc,
                block.EndUtc,
                block.Reason));
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
