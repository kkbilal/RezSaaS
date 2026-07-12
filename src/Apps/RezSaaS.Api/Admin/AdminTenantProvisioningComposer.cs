using System.Security.Claims;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Identity.Application;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.TenantManagement.Application;
using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.Api.Admin;

public sealed class AdminTenantProvisioningComposer
{
    private const string InvalidRequest = "ADMIN_TENANT_PROVISIONING_INVALID";
    private const string InvalidRole = "ADMIN_TENANT_MEMBERSHIP_INVALID_ROLE";
    private const string InvalidStatus = "ADMIN_TENANT_INVALID_STATUS";
    private const string UserClosureActive = "ADMIN_TENANT_USER_CLOSURE_ACTIVE";
    private const string OwnerNotFound = "ADMIN_TENANT_OWNER_NOT_FOUND";
    private const string Unauthorized = "ADMIN_TENANT_PROVISIONING_UNAUTHORIZED";

    private readonly AddTenantMembershipService addTenantMembershipService;
    private readonly BusinessProvisioningService businessProvisioningService;
    private readonly AbuseWorkflowQueryService abuseWorkflowQueryService;
    private readonly ChangeTenantLifecycleService changeTenantLifecycleService;
    private readonly ChangeTenantMembershipStatusService changeTenantMembershipStatusService;
    private readonly CreateTenantWithOwnerService createTenantWithOwnerService;
    private readonly TenantControlPlaneQueryService queryService;
    private readonly UserAccountExistenceService userAccountExistenceService;

    public AdminTenantProvisioningComposer(
        CreateTenantWithOwnerService createTenantWithOwnerService,
        TenantControlPlaneQueryService queryService,
        AddTenantMembershipService addTenantMembershipService,
        ChangeTenantLifecycleService changeTenantLifecycleService,
        ChangeTenantMembershipStatusService changeTenantMembershipStatusService,
        UserAccountExistenceService userAccountExistenceService,
        AbuseWorkflowQueryService abuseWorkflowQueryService,
        BusinessProvisioningService businessProvisioningService)
    {
        this.createTenantWithOwnerService = createTenantWithOwnerService;
        this.queryService = queryService;
        this.addTenantMembershipService = addTenantMembershipService;
        this.changeTenantLifecycleService = changeTenantLifecycleService;
        this.changeTenantMembershipStatusService = changeTenantMembershipStatusService;
        this.userAccountExistenceService = userAccountExistenceService;
        this.abuseWorkflowQueryService = abuseWorkflowQueryService;
        this.businessProvisioningService = businessProvisioningService;
    }

    public async Task<AdminTenantAccessResult> GetTenantsAsync(
        string? search,
        string? status,
        int? take,
        CancellationToken cancellationToken = default)
    {
        if (!TenantControlPlaneQueryService.IsValidStatusOrEmpty(status))
        {
            return AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.BadRequest,
                InvalidStatus);
        }

        IReadOnlyCollection<TenantListItemView> tenants =
            await queryService.GetAsync(
                new TenantControlPlaneQuery(
                    search,
                    status,
                    take ?? 50),
                cancellationToken);

        return AdminTenantAccessResult.Success(
            tenants.Select(ToListItemResponse).ToArray());
    }

    public async Task<AdminTenantAccessResult> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        TenantDetailView? tenant =
            await queryService.GetByIdAsync(
                tenantId,
                cancellationToken);

        return tenant is null
            ? AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.NotFound,
                "ADMIN_TENANT_NOT_FOUND")
            : AdminTenantAccessResult.Success(ToDetailResponse(tenant));
    }

    public async Task<AdminTenantAccessResult> GetMembershipsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        TenantDetailView? tenant =
            await queryService.GetByIdAsync(
                tenantId,
                cancellationToken);

        if (tenant is null)
        {
            return AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.NotFound,
                "ADMIN_TENANT_NOT_FOUND");
        }

        return AdminTenantAccessResult.SuccessMemberships(
            tenant.Memberships.Select(ToMembershipResponse).ToArray());
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
            || string.IsNullOrWhiteSpace(request.CategoryKey)
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

        if (await abuseWorkflowQueryService.HasActiveClosureCaseAsync(
            request.OwnerUserAccountId,
            cancellationToken))
        {
            return AdminTenantProvisioningResult.Failure(
                AdminTenantProvisioningOutcome.Conflict,
                UserClosureActive);
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

        // ISLETMENIN PUBLIC KIMLIGINI (Business) BURADA OLUSTUR.
        //
        // Bu adim EKSIKTI ve urunun en buyuk lansman blokajiydi: tenant aciliyordu ama
        // Business hicbir zaman olusturulmuyordu. Sonucta owner sube bile acamiyordu
        // (BranchManagementService -> BUSINESS_NOT_FOUND) ve salon /kesfet'te hic
        // gorunmuyordu. Yani urun tek bir salonu bile onboard edemiyordu.
        //
        // Neden BURADA: Tenant = faturalama/guvenlik siniri; Business = salonun public
        // kimligi. Bu urunde 1 tenant = 1 isletme (BusinessProfileSettings tenant basina).
        // Ayrica /api/business/settings/profile'da bir GUNCELLEME ucu vardi ama YARATMA
        // ucu yoktu -- yani Business'in provisioning aninda dogmasi zaten amaclanmisti.
        //
        // Idempotent: tenant yaratildi ama Business yaratilamadiysa ayni istek tekrar
        // denenebilir; mevcut Business varsa yeniden yaratilmaz.
        BusinessProvisioningResult businessResult =
            await businessProvisioningService.CreateAsync(
                new CreateBusinessCommand(
                    actorUserAccountId,
                    result.TenantId!.Value,
                    request.Slug,
                    request.DisplayName,
                    request.CategoryKey),
                cancellationToken);

        if (!businessResult.Succeeded)
        {
            // Tenant olustu ama isletme olusmadi. Bu bir YARIM DURUM; sessizce "basarili"
            // demek, owner'i sube acamayacagi bir panele sokmak demektir. Acikca hata don.
            // Cagiran taraf ayni istegi tekrar deneyebilir (islem idempotent).
            return AdminTenantProvisioningResult.Failure(
                AdminTenantProvisioningOutcome.Unprocessable,
                businessResult.ErrorCode ?? BusinessProvisioningService.InvalidRequest);
        }

        return AdminTenantProvisioningResult.Created(
            new AdminTenantProvisioningResponse(
                result.TenantId!.Value,
                request.Slug.Trim(),
                request.DisplayName.Trim(),
                request.OwnerUserAccountId,
                businessResult.BusinessId!.Value));
    }

    public async Task<AdminTenantAccessResult> AddMembershipAsync(
        Guid tenantId,
        AdminTenantMembershipCreateRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.Unauthorized,
                Unauthorized);
        }

        if (request.UserAccountId == Guid.Empty
            || !TryParseRole(request.Role, out TenantMembershipRole role))
        {
            return AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.BadRequest,
                InvalidRole);
        }

        if (!await userAccountExistenceService.ExistsActiveAsync(
            request.UserAccountId,
            cancellationToken))
        {
            return AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.NotFound,
                "ADMIN_TENANT_MEMBERSHIP_USER_NOT_FOUND");
        }

        if (await abuseWorkflowQueryService.HasActiveClosureCaseAsync(
            request.UserAccountId,
            cancellationToken))
        {
            return AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.Conflict,
                UserClosureActive);
        }

        TenantMembershipCommandResult result =
            await addTenantMembershipService.AddAsync(
                new AddTenantMembershipCommand(
                    tenantId,
                    actorUserAccountId,
                    request.UserAccountId,
                    role,
                    request.BranchId),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapMembershipFailure(result.ErrorCode ?? InvalidRequest);
        }

        TenantMembershipView? membership =
            (await queryService.GetMembershipsAsync(tenantId, cancellationToken))
            .SingleOrDefault(entity => entity.Id == result.MembershipId);

        return membership is null
            ? AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.Unprocessable,
                "ADMIN_TENANT_MEMBERSHIP_NOT_FOUND")
            : AdminTenantAccessResult.SuccessMembership(ToMembershipResponse(membership));
    }

    public async Task<AdminTenantAccessResult> CloseTenantAsync(
        Guid tenantId,
        AdminTenantLifecycleChangeRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        return await ChangeTenantLifecycleAsync(
            tenantId,
            request,
            user,
            (command, token) => changeTenantLifecycleService.CloseAsync(command, token),
            cancellationToken);
    }

    public async Task<AdminTenantAccessResult> RevokeMembershipAsync(
        Guid tenantId,
        Guid membershipId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        return await ChangeMembershipStatusAsync(
            tenantId,
            membershipId,
            user,
            (command, token) => changeTenantMembershipStatusService.RevokeAsync(command, token),
            cancellationToken);
    }

    public async Task<AdminTenantAccessResult> ReactivateTenantAsync(
        Guid tenantId,
        AdminTenantLifecycleChangeRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        return await ChangeTenantLifecycleAsync(
            tenantId,
            request,
            user,
            (command, token) => changeTenantLifecycleService.ReactivateAsync(command, token),
            cancellationToken);
    }

    public async Task<AdminTenantAccessResult> SuspendMembershipAsync(
        Guid tenantId,
        Guid membershipId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        return await ChangeMembershipStatusAsync(
            tenantId,
            membershipId,
            user,
            (command, token) => changeTenantMembershipStatusService.SuspendAsync(command, token),
            cancellationToken);
    }

    public async Task<AdminTenantAccessResult> SuspendTenantAsync(
        Guid tenantId,
        AdminTenantLifecycleChangeRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        return await ChangeTenantLifecycleAsync(
            tenantId,
            request,
            user,
            (command, token) => changeTenantLifecycleService.SuspendAsync(command, token),
            cancellationToken);
    }

    private async Task<AdminTenantAccessResult> ChangeTenantLifecycleAsync(
        Guid tenantId,
        AdminTenantLifecycleChangeRequest request,
        ClaimsPrincipal user,
        Func<ChangeTenantLifecycleCommand, CancellationToken, Task<TenantLifecycleCommandResult>> change,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.Unauthorized,
                Unauthorized);
        }

        TenantLifecycleCommandResult result =
            await change(
                new ChangeTenantLifecycleCommand(
                    tenantId,
                    actorUserAccountId,
                    request.Reason),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapLifecycleFailure(result.ErrorCode ?? InvalidRequest);
        }

        TenantDetailView? tenant =
            await queryService.GetByIdAsync(
                tenantId,
                cancellationToken);

        return tenant is null
            ? AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.Unprocessable,
                "ADMIN_TENANT_NOT_FOUND")
            : AdminTenantAccessResult.Success(ToDetailResponse(tenant));
    }

    private async Task<AdminTenantAccessResult> ChangeMembershipStatusAsync(
        Guid tenantId,
        Guid membershipId,
        ClaimsPrincipal user,
        Func<ChangeTenantMembershipStatusCommand, CancellationToken, Task<TenantMembershipCommandResult>> change,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.Unauthorized,
                Unauthorized);
        }

        TenantMembershipCommandResult result =
            await change(
                new ChangeTenantMembershipStatusCommand(
                    tenantId,
                    membershipId,
                    actorUserAccountId),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapMembershipFailure(result.ErrorCode ?? InvalidRequest);
        }

        TenantMembershipView? membership =
            (await queryService.GetMembershipsAsync(tenantId, cancellationToken))
            .SingleOrDefault(entity => entity.Id == result.MembershipId);

        return membership is null
            ? AdminTenantAccessResult.Failure(
                AdminTenantProvisioningOutcome.Unprocessable,
                "ADMIN_TENANT_MEMBERSHIP_NOT_FOUND")
            : AdminTenantAccessResult.SuccessMembership(ToMembershipResponse(membership));
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

    private static AdminTenantAccessResult MapMembershipFailure(string errorCode)
    {
        AdminTenantProvisioningOutcome outcome = errorCode switch
        {
            "TENANT_NOT_FOUND" or "TENANT_MEMBERSHIP_NOT_FOUND" => AdminTenantProvisioningOutcome.NotFound,
            "TENANT_MEMBERSHIP_ALREADY_EXISTS"
                or "TENANT_LAST_OWNER_REQUIRED"
                or "TENANT_MEMBERSHIP_REVOKED" =>
                AdminTenantProvisioningOutcome.Conflict,
            "TENANT_MEMBERSHIP_INVALID" => AdminTenantProvisioningOutcome.BadRequest,
            _ => AdminTenantProvisioningOutcome.Unprocessable,
        };

        return AdminTenantAccessResult.Failure(outcome, errorCode);
    }

    private static AdminTenantAccessResult MapLifecycleFailure(string errorCode)
    {
        AdminTenantProvisioningOutcome outcome = errorCode switch
        {
            "TENANT_NOT_FOUND" => AdminTenantProvisioningOutcome.NotFound,
            "TENANT_CLOSED" => AdminTenantProvisioningOutcome.Conflict,
            "TENANT_LIFECYCLE_INVALID" => AdminTenantProvisioningOutcome.BadRequest,
            _ => AdminTenantProvisioningOutcome.Unprocessable,
        };

        return AdminTenantAccessResult.Failure(outcome, errorCode);
    }

    private static bool TryParseRole(
        string role,
        out TenantMembershipRole parsedRole)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            parsedRole = default;
            return false;
        }

        return Enum.TryParse(role, ignoreCase: true, out parsedRole)
            && Enum.IsDefined(parsedRole);
    }

    private static AdminTenantDetailResponse ToDetailResponse(TenantDetailView tenant)
    {
        return new AdminTenantDetailResponse(
            tenant.Id,
            tenant.Slug,
            tenant.DisplayName,
            tenant.Status.ToString(),
            tenant.CreatedAtUtc,
            tenant.SuspendedAtUtc,
            tenant.ClosedAtUtc,
            tenant.Memberships.Select(ToMembershipResponse).ToArray());
    }

    private static AdminTenantListItemResponse ToListItemResponse(TenantListItemView tenant)
    {
        return new AdminTenantListItemResponse(
            tenant.Id,
            tenant.Slug,
            tenant.DisplayName,
            tenant.Status.ToString(),
            tenant.CreatedAtUtc,
            tenant.SuspendedAtUtc,
            tenant.ClosedAtUtc,
            tenant.ActiveMembershipCount);
    }

    private static AdminTenantMembershipResponse ToMembershipResponse(TenantMembershipView membership)
    {
        return new AdminTenantMembershipResponse(
            membership.Id,
            membership.TenantId,
            membership.UserAccountId,
            membership.Role.ToString(),
            membership.Status.ToString(),
            membership.BranchId,
            membership.CreatedAtUtc);
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
