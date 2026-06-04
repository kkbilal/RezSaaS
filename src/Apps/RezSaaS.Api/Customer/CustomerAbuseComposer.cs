using System.Security.Claims;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Api.Customer;

public sealed class CustomerAbuseComposer
{
    private const string InvalidTargetType = "CUSTOMER_ABUSE_INVALID_TARGET_TYPE";
    private const string Unauthorized = "CUSTOMER_ABUSE_UNAUTHORIZED";

    private readonly CreateAbuseAppealService createAppealService;
    private readonly AbuseWorkflowQueryService queryService;

    public CustomerAbuseComposer(
        AbuseWorkflowQueryService queryService,
        CreateAbuseAppealService createAppealService)
    {
        this.queryService = queryService;
        this.createAppealService = createAppealService;
    }

    public async Task<CustomerAbuseAccessResult> GetOverviewAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return CustomerAbuseAccessResult.Failure(
                CustomerAbuseOutcome.Unauthorized,
                Unauthorized);
        }

        CustomerAbuseOverviewView overview =
            await queryService.GetCustomerOverviewAsync(
                userAccountId,
                cancellationToken: cancellationToken);

        return CustomerAbuseAccessResult.Success(ToOverviewResponse(overview));
    }

    public async Task<CustomerAbuseAccessResult> GetAppealAsync(
        Guid appealId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return CustomerAbuseAccessResult.Failure(
                CustomerAbuseOutcome.Unauthorized,
                Unauthorized);
        }

        CustomerAbuseAppealView? appeal =
            await queryService.GetCustomerAppealByIdAsync(
                userAccountId,
                appealId,
                cancellationToken);

        return appeal is null
            ? CustomerAbuseAccessResult.Failure(
                CustomerAbuseOutcome.NotFound,
                "ABUSE_APPEAL_NOT_FOUND")
            : CustomerAbuseAccessResult.Success(ToAppealResponse(appeal));
    }

    public async Task<CustomerAbuseAccessResult> CreateAppealAsync(
        CustomerCreateAbuseAppealRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return CustomerAbuseAccessResult.Failure(
                CustomerAbuseOutcome.Unauthorized,
                Unauthorized);
        }

        if (!TryParseTargetType(request.TargetType, out AbuseAppealTargetType targetType))
        {
            return CustomerAbuseAccessResult.Failure(
                CustomerAbuseOutcome.BadRequest,
                InvalidTargetType);
        }

        AbuseWorkflowCommandResult result =
            await createAppealService.CreateAsync(
                new CreateAbuseAppealCommand(
                    userAccountId,
                    targetType,
                    request.TargetId,
                    request.Statement),
                cancellationToken);

        if (!result.Succeeded)
        {
            return MapFailure(result.ErrorCode ?? "ABUSE_APPEAL_INVALID");
        }

        AbuseAppealView appeal =
            (await queryService.GetAppealByIdAsync(result.EntityId!.Value, cancellationToken))!;

        return result.Created
            ? CustomerAbuseAccessResult.Created(ToAppealResponse(appeal))
            : CustomerAbuseAccessResult.Success(ToAppealResponse(appeal));
    }

    private static CustomerAbuseAccessResult MapFailure(string errorCode)
    {
        CustomerAbuseOutcome outcome = errorCode switch
        {
            "ABUSE_APPEAL_INVALID" => CustomerAbuseOutcome.BadRequest,
            "ABUSE_APPEAL_TARGET_NOT_FOUND" => CustomerAbuseOutcome.NotFound,
            "ABUSE_APPEAL_OPEN_LIMIT_EXCEEDED" => CustomerAbuseOutcome.Conflict,
            "ABUSE_APPEAL_WINDOW_CLOSED" => CustomerAbuseOutcome.Conflict,
            _ => CustomerAbuseOutcome.Unprocessable,
        };

        return CustomerAbuseAccessResult.Failure(outcome, errorCode);
    }

    private static CustomerAbuseAppealResponse ToAppealResponse(AbuseAppealView appeal)
    {
        return new CustomerAbuseAppealResponse(
            appeal.Id,
            appeal.TargetType.ToString(),
            appeal.TargetId,
            appeal.Statement,
            appeal.Status.ToString(),
            appeal.CreatedAtUtc,
            appeal.ReviewedAtUtc);
    }

    private static CustomerAbuseAppealResponse ToAppealResponse(CustomerAbuseAppealView appeal)
    {
        return new CustomerAbuseAppealResponse(
            appeal.Id,
            appeal.TargetType.ToString(),
            appeal.TargetId,
            appeal.Statement,
            appeal.Status.ToString(),
            appeal.CreatedAtUtc,
            appeal.ReviewedAtUtc);
    }

    private static CustomerAbuseOverviewResponse ToOverviewResponse(CustomerAbuseOverviewView overview)
    {
        return new CustomerAbuseOverviewResponse(
            overview.Sanctions.Select(entity => new CustomerSanctionResponse(
                entity.Id,
                entity.Type.ToString(),
                entity.StartsAtUtc,
                entity.EndsAtUtc,
                entity.RevokedAtUtc,
                entity.IsActive)).ToArray(),
            overview.Strikes.Select(entity => new CustomerStrikeResponse(
                entity.Id,
                entity.ReasonCode.ToString(),
                entity.IssuedAtUtc,
                entity.ExpiresAtUtc,
                entity.RevokedAtUtc)).ToArray(),
            overview.Appeals.Select(ToAppealResponse).ToArray(),
            overview.ClosureCases.Select(entity => new CustomerAccountClosureCaseResponse(
                entity.Id,
                entity.CustomerNotice,
                entity.ProposedAtUtc,
                entity.EligibleForExecutionAtUtc,
                entity.Status.ToString(),
                entity.DecidedAtUtc,
                entity.ExecutedAtUtc)).ToArray());
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }

    private static bool TryParseTargetType(
        string targetType,
        out AbuseAppealTargetType parsedTargetType)
    {
        return Enum.TryParse(targetType, ignoreCase: true, out parsedTargetType)
            && Enum.IsDefined(parsedTargetType);
    }
}
