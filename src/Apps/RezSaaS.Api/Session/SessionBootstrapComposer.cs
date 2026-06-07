using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Security;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Session;

public sealed class SessionBootstrapComposer
{
    private readonly UserManager<UserAccount> userManager;
    private readonly UserTenantMembershipQueryService tenantMembershipQueryService;
    private readonly StepUpSessionService stepUpSessionService;

    public SessionBootstrapComposer(
        UserManager<UserAccount> userManager,
        UserTenantMembershipQueryService tenantMembershipQueryService,
        StepUpSessionService stepUpSessionService)
    {
        this.userManager = userManager;
        this.tenantMembershipQueryService = tenantMembershipQueryService;
        this.stepUpSessionService = stepUpSessionService;
    }

    public async Task<SessionBootstrapResponse?> CreateAsync(
        ClaimsPrincipal principal,
        string? stepUpToken,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(principal, out Guid userAccountId))
        {
            return null;
        }

        UserAccount? account = await userManager.FindByIdAsync(userAccountId.ToString());

        if (account is null)
        {
            return null;
        }

        HashSet<string> platformRoles = GetClaimValues(
            principal,
            ClaimTypes.Role,
            "role",
            "roles");
        IList<string> persistedRoles = await userManager.GetRolesAsync(account);

        foreach (string role in persistedRoles)
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                platformRoles.Add(role);
            }
        }

        HashSet<string> authenticationMethods = GetClaimValues(principal, "amr");
        DateTimeOffset? stepUpExpiresAtUtc = null;
        StepUpSessionView? stepUpSession = await stepUpSessionService.ValidateAsync(
            userAccountId,
            stepUpToken,
            StepUpSessionService.MethodMfa,
            cancellationToken);

        if (stepUpSession is not null)
        {
            authenticationMethods.Add(stepUpSession.Method);
            stepUpExpiresAtUtc = stepUpSession.ExpiresAtUtc;
        }

        IReadOnlyCollection<SessionTenantMembershipResponse> memberships =
            (await tenantMembershipQueryService.GetActiveMembershipsAsync(
                userAccountId,
                cancellationToken))
            .Select(entity => new SessionTenantMembershipResponse(
                entity.Id,
                entity.TenantId,
                entity.TenantSlug,
                entity.TenantDisplayName,
                entity.Role.ToString(),
                entity.BranchId))
            .ToList();

        return new SessionBootstrapResponse(
            new SessionAccountResponse(
                userAccountId,
                account.Email,
                account.EmailConfirmed,
                account.Status.ToString()),
            platformRoles.Order(StringComparer.Ordinal).ToList(),
            new SessionStepUpResponse(
                authenticationMethods.Contains(
                    StepUpSessionService.MethodMfa,
                    StringComparer.OrdinalIgnoreCase),
                authenticationMethods.Order(StringComparer.Ordinal).ToList(),
                stepUpExpiresAtUtc),
            memberships);
    }

    private static HashSet<string> GetClaimValues(
        ClaimsPrincipal principal,
        params string[] claimTypes)
    {
        HashSet<string> values = new(StringComparer.Ordinal);

        foreach (Claim claim in principal.Claims)
        {
            if (!claimTypes.Contains(claim.Type, StringComparer.Ordinal)
                || string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            values.Add(claim.Value);
        }

        return values;
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
