using System.Security.Claims;
using RezSaaS.Modules.Identity.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Admin;

public sealed class AdminTenantProvisioningComposer
{
    private const string InvalidRequest = "ADMIN_TENANT_PROVISIONING_INVALID";
    private const string OwnerNotFound = "ADMIN_TENANT_OWNER_NOT_FOUND";
    private const string Unauthorized = "ADMIN_TENANT_PROVISIONING_UNAUTHORIZED";

    private readonly CreateTenantWithOwnerService createTenantWithOwnerService;
    private readonly UserAccountExistenceService userAccountExistenceService;

    public AdminTenantProvisioningComposer(
        CreateTenantWithOwnerService createTenantWithOwnerService,
        UserAccountExistenceService userAccountExistenceService)
    {
        this.createTenantWithOwnerService = createTenantWithOwnerService;
        this.userAccountExistenceService = userAccountExistenceService;
    }

    public async Task<AdminTenantProvisioningResult> CreateTenantAsync(
        AdminTenantProvisioningRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return AdminTenantProvisioningResult.Failure(
                AdminTenantProvisioningOutcome.Unauthorized,
                Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(request.Slug)
            || string.IsNullOrWhiteSpace(request.DisplayName)
            || request.OwnerUserAccountId == Guid.Empty)
        {
            return AdminTenantProvisioningResult.Failure(
                AdminTenantProvisioningOutcome.BadRequest,
                InvalidRequest);
        }

        if (!await userAccountExistenceService.ExistsActiveAsync(
            request.OwnerUserAccountId,
            cancellationToken))
        {
            return AdminTenantProvisioningResult.Failure(
                AdminTenantProvisioningOutcome.NotFound,
                OwnerNotFound);
        }

        CreateTenantWithOwnerResult result =
            await createTenantWithOwnerService.CreateAsync(
                new CreateTenantWithOwnerCommand(
                    actorUserAccountId,
                    request.Slug,
                    request.DisplayName,
                    request.OwnerUserAccountId),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode ?? InvalidRequest);
        }

        return AdminTenantProvisioningResult.Created(
            new AdminTenantProvisioningResponse(
                result.TenantId!.Value,
                request.Slug.Trim(),
                request.DisplayName.Trim(),
                request.OwnerUserAccountId));
    }

    private static AdminTenantProvisioningResult MapFailure(string errorCode)
    {
        AdminTenantProvisioningOutcome outcome = errorCode switch
        {
            "TENANT_SLUG_ALREADY_EXISTS" => AdminTenantProvisioningOutcome.Conflict,
            "TENANT_PROVISIONING_INVALID" => AdminTenantProvisioningOutcome.BadRequest,
            _ => AdminTenantProvisioningOutcome.Unprocessable,
        };

        return AdminTenantProvisioningResult.Failure(outcome, errorCode);
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }
}
