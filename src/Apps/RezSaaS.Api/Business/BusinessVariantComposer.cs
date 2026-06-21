using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Catalog.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessVariantComposer
{
    private const string Forbidden = "BUSINESS_VARIANT_FORBIDDEN";
    private const string Unauthorized = "BUSINESS_VARIANT_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly ServiceVariantManagementService variantService;
    private readonly ServiceRequiredSkillService requiredSkillService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessVariantComposer(
        TenantBookingAuthorizationService authorizationService,
        ServiceVariantManagementService variantService,
        ServiceRequiredSkillService requiredSkillService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.variantService = variantService;
        this.requiredSkillService = requiredSkillService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    private async Task<bool> CanManageAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        if (tenantContextAccessor.TenantId is not { } tid) return false;
        if (!TryGetUserAccountId(user, out Guid uid)) return false;
        return await authorizationService.CanManageBusinessSettingsAsync(tid, uid, ct);
    }

    public async Task<BusinessVariantResult> ListByServiceAsync(ClaimsPrincipal user, Guid serviceId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Forbidden, Forbidden);

        var result = await variantService.ListByServiceAsync(serviceId, ct);
        return ToListResult(result);
    }

    public async Task<BusinessVariantResult> GetByIdAsync(ClaimsPrincipal user, Guid variantId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Forbidden, Forbidden);

        return ToResult(await variantService.GetByIdAsync(variantId, ct));
    }

    public async Task<BusinessVariantResult> CreateAsync(ClaimsPrincipal user, Guid serviceId, BusinessVariantCreateRequest req, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Forbidden, Forbidden);

        return ToResult(await variantService.CreateAsync(new CreateServiceVariantCommand(
            uid, serviceId, req.Name, req.DurationMinutes, req.PriceAmount, req.CurrencyCode, req.RequiredResourceTypeId), ct));
    }

    public async Task<BusinessVariantResult> UpdateAsync(ClaimsPrincipal user, Guid serviceId, Guid variantId, BusinessVariantUpdateRequest req, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Forbidden, Forbidden);

        return ToResult(await variantService.UpdateAsync(new UpdateServiceVariantCommand(
            uid, serviceId, variantId, req.Name, req.DurationMinutes, req.PriceAmount, req.CurrencyCode, req.RequiredResourceTypeId), ct));
    }

    public async Task<BusinessVariantResult> DeleteAsync(ClaimsPrincipal user, Guid variantId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Forbidden, Forbidden);

        return ToResult(await variantService.DeleteAsync(uid, variantId, ct));
    }

    public async Task<BusinessVariantResult> AssignRequiredSkillAsync(ClaimsPrincipal user, Guid variantId, Guid skillId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out Guid uid))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Forbidden, Forbidden);

        var result = await requiredSkillService.AssignAsync(uid, variantId, skillId, ct);
        return ToSkillResult(result);
    }

    public async Task<BusinessVariantResult> RemoveRequiredSkillAsync(ClaimsPrincipal user, Guid variantId, Guid skillId, CancellationToken ct = default)
    {
        if (!TryGetUserAccountId(user, out _))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Unauthorized, Unauthorized);
        if (!await CanManageAsync(user, ct))
            return BusinessVariantResult.Failure(BusinessVariantOutcome.Forbidden, Forbidden);

        var result = await requiredSkillService.RemoveAsync(variantId, skillId, ct);
        return ToSkillResult(result);
    }

    private static BusinessVariantResult ToResult(ServiceVariantManagementResult r)
    {
        if (r.Succeeded && r.Variant is not null)
            return BusinessVariantResult.Success(ToResponse(r.Variant));

        return BusinessVariantResult.Failure(r.ErrorCode switch
        {
            ServiceVariantManagementService.InvalidRequest => BusinessVariantOutcome.BadRequest,
            ServiceVariantManagementService.ServiceNotFound => BusinessVariantOutcome.NotFound,
            ServiceVariantManagementService.VariantNotFound => BusinessVariantOutcome.NotFound,
            ServiceVariantManagementService.NameConflict => BusinessVariantOutcome.Conflict,
            _ => BusinessVariantOutcome.BadRequest,
        }, r.ErrorCode ?? "VARIANT_FAILED");
    }

    private static BusinessVariantResult ToListResult(ServiceVariantManagementResult r)
    {
        if (r.Succeeded && r.Variants is not null)
            return BusinessVariantResult.SuccessList(r.Variants.Select(ToResponse).ToList());
        return BusinessVariantResult.Failure(BusinessVariantOutcome.BadRequest, r.ErrorCode ?? "VARIANT_FAILED");
    }

    private static BusinessVariantResult ToSkillResult(ServiceRequiredSkillActionResult r)
    {
        if (r.Succeeded) return BusinessVariantResult.SuccessList([]);

        return BusinessVariantResult.Failure(r.ErrorCode switch
        {
            ServiceRequiredSkillService.VariantNotFound => BusinessVariantOutcome.NotFound,
            ServiceRequiredSkillService.AlreadyAssigned => BusinessVariantOutcome.Conflict,
            ServiceRequiredSkillService.NotAssigned => BusinessVariantOutcome.NotFound,
            _ => BusinessVariantOutcome.BadRequest,
        }, r.ErrorCode ?? "REQUIRED_SKILL_FAILED");
    }

    private static BusinessVariantResponse ToResponse(ServiceVariantView v)
        => new(v.Id, v.ServiceId, v.Name, v.DurationMinutes, v.PriceAmount, v.CurrencyCode, v.RequiredResourceTypeId, v.CreatedAtUtc);

    private static bool TryGetUserAccountId(ClaimsPrincipal user, out Guid uid)
    {
        string? raw = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out uid);
    }
}
