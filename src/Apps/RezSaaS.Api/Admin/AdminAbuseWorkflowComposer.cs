using System.Security.Claims;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Identity.Application;
using RezSaaS.Modules.Messaging.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Admin;

public sealed class AdminAbuseWorkflowComposer
{
    private const string ActiveMembershipExists = "ACCOUNT_CLOSURE_ACTIVE_TENANT_MEMBERSHIP";
    private const string IdentityIneligible = "ACCOUNT_CLOSURE_IDENTITY_INELIGIBLE";
    private const string InvalidAppealStatus = "ABUSE_APPEAL_INVALID_STATUS";
    private const string InvalidClosureStatus = "ACCOUNT_CLOSURE_INVALID_STATUS";
    private const string Unauthorized = "ADMIN_ABUSE_UNAUTHORIZED";
    private const string UserNotFound = "ACCOUNT_CLOSURE_USER_NOT_FOUND";

    private readonly AccountClosureExecutionService accountClosureExecutionService;
    private readonly AbuseWorkflowQueryService queryService;
    private readonly PlatformTransactionalMessageQueueService platformMessageQueueService;
    private readonly ProposeAccountClosureService proposeAccountClosureService;
    private readonly ReviewAbuseAppealService reviewAbuseAppealService;
    private readonly ReviewAccountClosureService reviewAccountClosureService;
    private readonly UserAccountClosureService userAccountClosureService;
    private readonly UserTenantMembershipQueryService userTenantMembershipQueryService;

    public AdminAbuseWorkflowComposer(
        AbuseWorkflowQueryService queryService,
        ReviewAbuseAppealService reviewAbuseAppealService,
        ProposeAccountClosureService proposeAccountClosureService,
        ReviewAccountClosureService reviewAccountClosureService,
        AccountClosureExecutionService accountClosureExecutionService,
        UserAccountClosureService userAccountClosureService,
        UserTenantMembershipQueryService userTenantMembershipQueryService,
        PlatformTransactionalMessageQueueService platformMessageQueueService)
    {
        this.queryService = queryService;
        this.reviewAbuseAppealService = reviewAbuseAppealService;
        this.proposeAccountClosureService = proposeAccountClosureService;
        this.reviewAccountClosureService = reviewAccountClosureService;
        this.accountClosureExecutionService = accountClosureExecutionService;
        this.userAccountClosureService = userAccountClosureService;
        this.userTenantMembershipQueryService = userTenantMembershipQueryService;
        this.platformMessageQueueService = platformMessageQueueService;
    }

    public async Task<AdminAbuseWorkflowAccessResult> GetAppealsAsync(
        Guid? userAccountId,
        string? status,
        int? take,
        CancellationToken cancellationToken = default)
    {
        if (!AbuseWorkflowQueryService.IsValidAppealStatusOrEmpty(status))
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.BadRequest,
                InvalidAppealStatus);
        }

        IReadOnlyCollection<AbuseAppealView> appeals =
            await queryService.GetAppealsAsync(
                userAccountId,
                status,
                take ?? 50,
                cancellationToken);

        return AdminAbuseWorkflowAccessResult.Success(
            appeals.Select(ToAppealResponse).ToArray());
    }

    public async Task<AdminAbuseWorkflowAccessResult> GetAppealAsync(
        Guid appealId,
        CancellationToken cancellationToken = default)
    {
        AbuseAppealView? appeal =
            await queryService.GetAppealByIdAsync(appealId, cancellationToken);

        return appeal is null
            ? AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.NotFound,
                "ABUSE_APPEAL_NOT_FOUND")
            : AdminAbuseWorkflowAccessResult.Success(ToAppealResponse(appeal));
    }

    public async Task<AdminAbuseWorkflowAccessResult> GetClosureCasesAsync(
        Guid? userAccountId,
        string? status,
        int? take,
        CancellationToken cancellationToken = default)
    {
        if (!AbuseWorkflowQueryService.IsValidClosureStatusOrEmpty(status))
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.BadRequest,
                InvalidClosureStatus);
        }

        IReadOnlyCollection<AccountClosureCaseView> closureCases =
            await queryService.GetClosureCasesAsync(
                userAccountId,
                status,
                take ?? 50,
                cancellationToken);

        return AdminAbuseWorkflowAccessResult.Success(
            closureCases.Select(ToClosureCaseResponse).ToArray());
    }

    public async Task<AdminAbuseWorkflowAccessResult> GetClosureCaseAsync(
        Guid closureCaseId,
        CancellationToken cancellationToken = default)
    {
        AccountClosureCaseView? closureCase =
            await queryService.GetClosureCaseByIdAsync(closureCaseId, cancellationToken);

        return closureCase is null
            ? AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.NotFound,
                "ACCOUNT_CLOSURE_NOT_FOUND")
            : AdminAbuseWorkflowAccessResult.Success(ToClosureCaseResponse(closureCase));
    }

    public async Task<AdminAbuseWorkflowAccessResult> ReviewAppealAsync(
        Guid appealId,
        AbuseAppealStatus decision,
        AdminReviewAbuseAppealRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return UnauthorizedResult();
        }

        AbuseWorkflowCommandResult result =
            await reviewAbuseAppealService.ReviewAsync(
                new ReviewAbuseAppealCommand(
                    actorUserAccountId,
                    appealId,
                    decision,
                    request.Reason),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode!);
        }

        AbuseAppealView appeal =
            (await queryService.GetAppealByIdAsync(appealId, cancellationToken))!;
        await platformMessageQueueService.EnqueueAsync(
            PlatformAbuseNotificationContent.CreateAppealDecision(appeal),
            cancellationToken);

        return AdminAbuseWorkflowAccessResult.Success(ToAppealResponse(appeal));
    }

    public async Task<AdminAbuseWorkflowAccessResult> ProposeClosureAsync(
        Guid userAccountId,
        AdminAccountClosureProposalRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return UnauthorizedResult();
        }

        UserAccountClosureEligibilityView? eligibility =
            await userAccountClosureService.GetEligibilityAsync(userAccountId, cancellationToken);

        if (eligibility is null)
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.NotFound,
                UserNotFound);
        }

        if (!eligibility.CanProposeClosure)
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.Conflict,
                IdentityIneligible);
        }

        if (await userTenantMembershipQueryService.HasActiveMembershipsAsync(
            userAccountId,
            cancellationToken))
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.Conflict,
                ActiveMembershipExists);
        }

        AbuseWorkflowCommandResult result =
            await proposeAccountClosureService.ProposeAsync(
                new ProposeAccountClosureCommand(
                    actorUserAccountId,
                    userAccountId,
                    request.InternalReason,
                    request.CustomerNotice),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode!);
        }

        AccountClosureCaseView closureCase =
            (await queryService.GetClosureCaseByIdAsync(result.EntityId!.Value, cancellationToken))!;
        await platformMessageQueueService.EnqueueAsync(
            PlatformAbuseNotificationContent.CreateClosureProposal(closureCase),
            cancellationToken);

        return result.Created
            ? AdminAbuseWorkflowAccessResult.Created(ToClosureCaseResponse(closureCase))
            : AdminAbuseWorkflowAccessResult.Success(ToClosureCaseResponse(closureCase));
    }

    public async Task<AdminAbuseWorkflowAccessResult> ReviewClosureAsync(
        Guid closureCaseId,
        AccountClosureCaseStatus decision,
        AdminAccountClosureReviewRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return UnauthorizedResult();
        }

        AbuseWorkflowCommandResult result =
            await reviewAccountClosureService.ReviewAsync(
                new ReviewAccountClosureCommand(
                    actorUserAccountId,
                    closureCaseId,
                    decision,
                    request.Reason),
                cancellationToken);

        return result.Succeeded
            ? AdminAbuseWorkflowAccessResult.Success(
                ToClosureCaseResponse(
                    (await queryService.GetClosureCaseByIdAsync(closureCaseId, cancellationToken))!))
            : MapFailure(result.ErrorCode!);
    }

    public async Task<AdminAbuseWorkflowAccessResult> ExecuteClosureAsync(
        Guid closureCaseId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid actorUserAccountId))
        {
            return UnauthorizedResult();
        }

        AccountClosureCaseView? closureCase =
            await queryService.GetClosureCaseByIdAsync(closureCaseId, cancellationToken);

        if (closureCase is null)
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.NotFound,
                "ACCOUNT_CLOSURE_NOT_FOUND");
        }

        UserAccountClosureEligibilityView? eligibility =
            await userAccountClosureService.GetEligibilityAsync(
                closureCase.UserAccountId,
                cancellationToken);

        if (eligibility is null)
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.NotFound,
                UserNotFound);
        }

        if (!eligibility.CanExecuteClosure)
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.Conflict,
                IdentityIneligible);
        }

        if (await userTenantMembershipQueryService.HasActiveMembershipsAsync(
            closureCase.UserAccountId,
            cancellationToken))
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.Conflict,
                ActiveMembershipExists);
        }

        ExecuteAccountClosureCommand command =
            new(actorUserAccountId, closureCaseId);
        AbuseWorkflowCommandResult beginResult =
            await accountClosureExecutionService.BeginAsync(command, cancellationToken);

        if (!beginResult.Succeeded)
        {
            return MapFailure(beginResult.ErrorCode!);
        }

        if (await userTenantMembershipQueryService.HasActiveMembershipsAsync(
            closureCase.UserAccountId,
            cancellationToken))
        {
            return AdminAbuseWorkflowAccessResult.Failure(
                AdminAbuseOutcome.Conflict,
                ActiveMembershipExists);
        }

        UserAccountClosureResult identityResult =
            await userAccountClosureService.CloseAsync(
                actorUserAccountId,
                closureCase.UserAccountId,
                closureCaseId,
                cancellationToken);

        if (!identityResult.Succeeded)
        {
            return MapFailure(identityResult.ErrorCode!);
        }

        AbuseWorkflowCommandResult completeResult =
            await accountClosureExecutionService.CompleteAsync(command, cancellationToken);

        return completeResult.Succeeded
            ? AdminAbuseWorkflowAccessResult.Success(
                ToClosureCaseResponse(
                    (await queryService.GetClosureCaseByIdAsync(closureCaseId, cancellationToken))!))
            : MapFailure(completeResult.ErrorCode!);
    }

    private static AdminAbuseWorkflowAccessResult MapFailure(string errorCode)
    {
        AdminAbuseOutcome outcome = errorCode switch
        {
            "ABUSE_APPEAL_INVALID" => AdminAbuseOutcome.BadRequest,
            "ABUSE_APPEAL_REVIEW_INVALID" => AdminAbuseOutcome.BadRequest,
            "ACCOUNT_CLOSURE_PROPOSAL_INVALID" => AdminAbuseOutcome.BadRequest,
            "ACCOUNT_CLOSURE_REVIEW_INVALID" => AdminAbuseOutcome.BadRequest,
            "ACCOUNT_CLOSURE_EXECUTION_INVALID" => AdminAbuseOutcome.BadRequest,
            "ABUSE_APPEAL_NOT_FOUND" => AdminAbuseOutcome.NotFound,
            "ABUSE_APPEAL_TARGET_NOT_FOUND" => AdminAbuseOutcome.NotFound,
            "ACCOUNT_CLOSURE_NOT_FOUND" => AdminAbuseOutcome.NotFound,
            "USER_ACCOUNT_CLOSURE_NOT_FOUND" => AdminAbuseOutcome.NotFound,
            "ABUSE_APPEAL_ALREADY_REVIEWED" => AdminAbuseOutcome.Conflict,
            "ABUSE_APPEAL_SELF_REVIEW_FORBIDDEN" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_ACTIVE_CASE_EXISTS" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_ALREADY_REVIEWED" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_REQUIRES_SECOND_ADMIN" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_NOT_APPROVED" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_APPEAL_WINDOW_OPEN" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_APPEAL_PENDING" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_NOTICE_NOT_DELIVERED" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_RISK_NO_LONGER_HIGH" => AdminAbuseOutcome.Conflict,
            "ACCOUNT_CLOSURE_EXECUTION_DISABLED" => AdminAbuseOutcome.Conflict,
            "USER_ACCOUNT_CLOSURE_HAS_PLATFORM_ROLE" => AdminAbuseOutcome.Conflict,
            "USER_ACCOUNT_CLOSURE_NOT_ACTIVE" => AdminAbuseOutcome.Conflict,
            _ => AdminAbuseOutcome.Unprocessable,
        };

        return AdminAbuseWorkflowAccessResult.Failure(outcome, errorCode);
    }

    private static AdminAbuseAppealResponse ToAppealResponse(AbuseAppealView appeal)
    {
        return new AdminAbuseAppealResponse(
            appeal.Id,
            appeal.UserAccountId,
            appeal.TargetType.ToString(),
            appeal.TargetId,
            appeal.Statement,
            appeal.Status.ToString(),
            appeal.CreatedAtUtc,
            appeal.ReviewedAtUtc,
            appeal.ReviewedByUserAccountId,
            appeal.ReviewReason);
    }

    private static AdminAccountClosureCaseResponse ToClosureCaseResponse(AccountClosureCaseView closureCase)
    {
        return new AdminAccountClosureCaseResponse(
            closureCase.Id,
            closureCase.UserAccountId,
            closureCase.ProposedByUserAccountId,
            closureCase.InternalReason,
            closureCase.CustomerNotice,
            closureCase.ProposedAtUtc,
            closureCase.CustomerNoticeDeliveredAtUtc,
            closureCase.EligibleForExecutionAtUtc,
            closureCase.Status.ToString(),
            closureCase.ReviewedByUserAccountId,
            closureCase.DecisionReason,
            closureCase.DecidedAtUtc,
            closureCase.ExecutionStartedByUserAccountId,
            closureCase.ExecutionStartedAtUtc,
            closureCase.ExecutedByUserAccountId,
            closureCase.ExecutedAtUtc);
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }

    private static AdminAbuseWorkflowAccessResult UnauthorizedResult()
    {
        return AdminAbuseWorkflowAccessResult.Failure(
            AdminAbuseOutcome.Unauthorized,
            Unauthorized);
    }
}
