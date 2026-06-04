using System.Security.Claims;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Identity.Application;

namespace RezSaaS.Api.Admin;

public sealed class AdminAbuseControlPlaneComposer
{
    private const string InvalidRequest = "ADMIN_ABUSE_INVALID";
    private const string InvalidSeverity = "ADMIN_ABUSE_INVALID_SEVERITY";
    private const string InvalidSanctionType = "ADMIN_ABUSE_INVALID_SANCTION_TYPE";
    private const string Unauthorized = "ADMIN_ABUSE_UNAUTHORIZED";
    private const string UserNotFound = "ADMIN_ABUSE_USER_NOT_FOUND";

    private readonly ApplyUserSanctionService applyUserSanctionService;
    private readonly AbuseControlPlaneQueryService queryService;
    private readonly RevokeUserSanctionService revokeUserSanctionService;
    private readonly UserAccountExistenceService userAccountExistenceService;

    public AdminAbuseControlPlaneComposer(
        AbuseControlPlaneQueryService queryService,
        ApplyUserSanctionService applyUserSanctionService,
        RevokeUserSanctionService revokeUserSanctionService,
        UserAccountExistenceService userAccountExistenceService)
    {
        this.queryService = queryService;
        this.applyUserSanctionService = applyUserSanctionService;
        this.revokeUserSanctionService = revokeUserSanctionService;
        this.userAccountExistenceService = userAccountExistenceService;
    }

    public async Task<AdminAbuseAccessResult> GetEventsAsync(
        Guid? userAccountId,
        Guid? tenantId,
        string? severity,
        int? take,
        CancellationToken cancellationToken = default)
    {
        if (!AbuseControlPlaneQueryService.IsValidSeverityOrEmpty(severity))
        {
            return AdminAbuseAccessResult.Failure(
                AdminAbuseOutcome.BadRequest,
                InvalidSeverity);
        }

        IReadOnlyCollection<AbuseEventView> events =
            await queryService.GetEventsAsync(
                new AbuseControlPlaneQuery(
                    userAccountId,
                    tenantId,
                    severity,
                    take ?? 50),
                cancellationToken);

        return AdminAbuseAccessResult.Success(
            events.Select(ToEventResponse).ToArray());
    }

    public async Task<AdminAbuseAccessResult> GetUserOverviewAsync(
        Guid userAccountId,
        int? take,
        CancellationToken cancellationToken = default)
    {
        UserAbuseOverviewView? overview =
            await queryService.GetUserOverviewAsync(
                userAccountId,
                take ?? 50,
                cancellationToken);

        return overview is null
            ? AdminAbuseAccessResult.Failure(
                AdminAbuseOutcome.BadRequest,
                InvalidRequest)
            : AdminAbuseAccessResult.Success(ToOverviewResponse(overview));
    }

    public async Task<AdminAbuseAccessResult> ApplySanctionAsync(
        Guid userAccountId,
        AdminApplyUserSanctionRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return AdminAbuseAccessResult.Failure(
                AdminAbuseOutcome.Unauthorized,
                Unauthorized);
        }

        if (!TryParseSanctionType(request.Type, out UserSanctionType sanctionType))
        {
            return AdminAbuseAccessResult.Failure(
                AdminAbuseOutcome.BadRequest,
                InvalidSanctionType);
        }

        if (!await userAccountExistenceService.ExistsActiveAsync(
            userAccountId,
            cancellationToken))
        {
            return AdminAbuseAccessResult.Failure(
                AdminAbuseOutcome.NotFound,
                UserNotFound);
        }

        ApplyUserSanctionResult result =
            await applyUserSanctionService.ApplyAsync(
                new ApplyUserSanctionCommand(
                    actorUserAccountId,
                    userAccountId,
                    sanctionType,
                    request.Reason,
                    request.EndsAtUtc),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode ?? InvalidRequest);
        }

        UserAbuseOverviewView overview =
            (await queryService.GetUserOverviewAsync(
                userAccountId,
                cancellationToken: cancellationToken))!;
        UserSanctionView? sanction = overview.Sanctions
            .SingleOrDefault(entity => entity.Id == result.SanctionId);

        return sanction is null
            ? AdminAbuseAccessResult.Failure(
                AdminAbuseOutcome.Unprocessable,
                "ADMIN_ABUSE_SANCTION_NOT_FOUND")
            : AdminAbuseAccessResult.Created(ToSanctionResponse(sanction));
    }

    public async Task<AdminAbuseAccessResult> RevokeSanctionAsync(
        Guid userAccountId,
        Guid sanctionId,
        AdminRevokeUserSanctionRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return AdminAbuseAccessResult.Failure(
                AdminAbuseOutcome.Unauthorized,
                Unauthorized);
        }

        ApplyUserSanctionResult result =
            await revokeUserSanctionService.RevokeAsync(
                new RevokeUserSanctionCommand(
                    actorUserAccountId,
                    userAccountId,
                    sanctionId,
                    request.Reason),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode ?? InvalidRequest);
        }

        UserAbuseOverviewView overview =
            (await queryService.GetUserOverviewAsync(
                userAccountId,
                cancellationToken: cancellationToken))!;
        UserSanctionView? sanction = overview.Sanctions
            .SingleOrDefault(entity => entity.Id == result.SanctionId);

        return sanction is null
            ? AdminAbuseAccessResult.Failure(
                AdminAbuseOutcome.NotFound,
                "ADMIN_ABUSE_SANCTION_NOT_FOUND")
            : AdminAbuseAccessResult.SuccessSanction(ToSanctionResponse(sanction));
    }

    private static AdminAbuseAccessResult MapFailure(string errorCode)
    {
        AdminAbuseOutcome outcome = errorCode switch
        {
            "USER_SANCTION_INVALID" => AdminAbuseOutcome.BadRequest,
            "USER_SANCTION_REVOCATION_INVALID" => AdminAbuseOutcome.BadRequest,
            "USER_ACTIVE_SANCTION_EXISTS" => AdminAbuseOutcome.Conflict,
            "USER_SANCTION_NOT_FOUND" => AdminAbuseOutcome.NotFound,
            "USER_SANCTION_NOT_REVOCABLE" => AdminAbuseOutcome.Conflict,
            "USER_PERMANENT_CLOSURE_REQUIRES_ACCOUNT_WORKFLOW" => AdminAbuseOutcome.Unprocessable,
            _ => AdminAbuseOutcome.Unprocessable,
        };

        return AdminAbuseAccessResult.Failure(outcome, errorCode);
    }

    private static AdminAbuseEventResponse ToEventResponse(AbuseEventView abuseEvent)
    {
        return new AdminAbuseEventResponse(
            abuseEvent.Id,
            abuseEvent.TenantId,
            abuseEvent.UserAccountId,
            abuseEvent.EventType,
            abuseEvent.Severity.ToString(),
            abuseEvent.DetailsJson,
            abuseEvent.OccurredAtUtc);
    }

    private static AdminUserAbuseOverviewResponse ToOverviewResponse(UserAbuseOverviewView overview)
    {
        return new AdminUserAbuseOverviewResponse(
            overview.UserAccountId,
            overview.Events.Select(ToEventResponse).ToArray(),
            overview.Sanctions.Select(ToSanctionResponse).ToArray(),
            overview.Reports.Select(AdminAbuseReportComposer.ToReportResponse).ToArray(),
            overview.Strikes.Select(AdminAbuseReportComposer.ToStrikeResponse).ToArray(),
            new AdminUserRiskResponse(
                overview.Risk.ActiveStrikeCount,
                overview.Risk.Level.ToString()));
    }

    private static AdminUserSanctionResponse ToSanctionResponse(UserSanctionView sanction)
    {
        return new AdminUserSanctionResponse(
            sanction.Id,
            sanction.UserAccountId,
            sanction.Type.ToString(),
            sanction.Reason,
            sanction.StartsAtUtc,
            sanction.EndsAtUtc,
            sanction.RevokedAtUtc,
            sanction.RevokedByUserAccountId,
            sanction.RevocationReason,
            sanction.IsActive);
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }

    private static bool TryParseSanctionType(
        string type,
        out UserSanctionType sanctionType)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            sanctionType = default;
            return false;
        }

        return Enum.TryParse(type, ignoreCase: true, out sanctionType)
            && Enum.IsDefined(sanctionType);
    }
}
