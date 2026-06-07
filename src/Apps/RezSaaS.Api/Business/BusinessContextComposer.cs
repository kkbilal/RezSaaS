using System.Security.Claims;
using RezSaaS.Modules.TenantManagement.Application;
using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.Api.Business;

public sealed class BusinessContextComposer
{
    private readonly UserTenantMembershipQueryService tenantMembershipQueryService;

    public BusinessContextComposer(UserTenantMembershipQueryService tenantMembershipQueryService)
    {
        this.tenantMembershipQueryService = tenantMembershipQueryService;
    }

    public async Task<BusinessContextResponse?> CreateAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(principal, out Guid userAccountId))
        {
            return null;
        }

        IReadOnlyCollection<UserTenantMembershipView> memberships =
            await tenantMembershipQueryService.GetActiveMembershipsAsync(
                userAccountId,
                cancellationToken);

        IReadOnlyCollection<BusinessTenantContextResponse> tenants = memberships
            .Select(entity => new BusinessTenantContextResponse(
                entity.Id,
                entity.TenantId,
                entity.TenantSlug,
                entity.TenantDisplayName,
                entity.Role.ToString(),
                entity.BranchId,
                entity.BranchId is null,
                CreateCapabilities(entity.Role)))
            .ToList();

        return new BusinessContextResponse(tenants);
    }

    private static IReadOnlyCollection<string> CreateCapabilities(TenantMembershipRole role)
    {
        return role switch
        {
            TenantMembershipRole.BusinessOwner =>
            [
                BusinessCapabilityNames.ManageAppointmentRequests,
                BusinessCapabilityNames.ReportAppointmentRequestAbuse,
            ],
            TenantMembershipRole.BranchManager =>
            [
                BusinessCapabilityNames.ManageAppointmentRequests,
                BusinessCapabilityNames.ReportAppointmentRequestAbuse,
            ],
            _ => [],
        };
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal principal,
        out Guid userAccountId)
    {
        string? rawUserAccountId = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserAccountId, out userAccountId);
    }
}
