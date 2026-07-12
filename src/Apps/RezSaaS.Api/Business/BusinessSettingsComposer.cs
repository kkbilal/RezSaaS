using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessSettingsComposer
{
    private const string Forbidden = "BUSINESS_SETTINGS_FORBIDDEN";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string Unauthorized = "BUSINESS_SETTINGS_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly BusinessProfileSettingsService profileSettingsService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessSettingsComposer(
        TenantBookingAuthorizationService authorizationService,
        BusinessProfileSettingsService profileSettingsService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.profileSettingsService = profileSettingsService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessSettingsResult> GetProfileAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessSettingsResult.Failure(
                BusinessSettingsOutcome.Unauthorized,
                Unauthorized);
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessSettingsResult.Failure(
                BusinessSettingsOutcome.BadRequest,
                MissingTenantContext);
        }

        if (!await authorizationService.CanManageBusinessSettingsAsync(
            tenantId,
            userAccountId,
            cancellationToken))
        {
            return BusinessSettingsResult.Failure(
                BusinessSettingsOutcome.Forbidden,
                Forbidden);
        }

        return ToResult(await profileSettingsService.GetCurrentAsync(cancellationToken));
    }

    public async Task<BusinessSettingsResult> UpdateProfileAsync(
        ClaimsPrincipal user,
        BusinessProfileSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessSettingsResult.Failure(
                BusinessSettingsOutcome.Unauthorized,
                Unauthorized);
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessSettingsResult.Failure(
                BusinessSettingsOutcome.BadRequest,
                MissingTenantContext);
        }

        if (!await authorizationService.CanManageBusinessSettingsAsync(
            tenantId,
            userAccountId,
            cancellationToken))
        {
            return BusinessSettingsResult.Failure(
                BusinessSettingsOutcome.Forbidden,
                Forbidden);
        }

        BusinessProfileSettingsResult result = await profileSettingsService.UpdateAsync(
            new UpdateBusinessProfileSettingsCommand(
                userAccountId,
                request.DisplayName ?? string.Empty,
                request.Description ?? string.Empty,
                request.PublicRules ?? string.Empty,
                request.SeoTitle ?? string.Empty,
                request.SeoDescription ?? string.Empty,
                request.StaffDisplayPolicy ?? string.Empty,
                request.CancellationCutoffHours),
            cancellationToken);

        return ToResult(result);
    }

    private static BusinessSettingsResult ToResult(BusinessProfileSettingsResult result)
    {
        if (result.Succeeded)
        {
            return BusinessSettingsResult.Success(ToResponse(result.Settings!));
        }

        BusinessSettingsOutcome outcome = result.ErrorCode switch
        {
            BusinessProfileSettingsService.InvalidRequest => BusinessSettingsOutcome.BadRequest,
            BusinessProfileSettingsService.MissingTenantContext => BusinessSettingsOutcome.BadRequest,
            BusinessProfileSettingsService.NotFound => BusinessSettingsOutcome.NotFound,
            BusinessProfileSettingsService.MultipleBusinessesUnsupported => BusinessSettingsOutcome.Conflict,
            _ => BusinessSettingsOutcome.BadRequest,
        };

        return BusinessSettingsResult.Failure(
            outcome,
            result.ErrorCode ?? "BUSINESS_SETTINGS_FAILED");
    }

    private static BusinessProfileSettingsResponse ToResponse(
        BusinessProfileSettingsView settings)
    {
        return new BusinessProfileSettingsResponse(
            settings.BusinessId,
            settings.Slug,
            settings.DisplayName,
            settings.CategoryKey,
            settings.Description,
            settings.PublicRules,
            settings.SeoTitle,
            settings.SeoDescription,
            settings.StaffDisplayPolicy);
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
