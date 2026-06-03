using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessAppointmentRequestComposer
{
    private const string Forbidden = "BUSINESS_APPOINTMENT_REQUEST_FORBIDDEN";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "BUSINESS_APPOINTMENT_REQUEST_NOT_FOUND";
    private const string Unauthorized = "BUSINESS_APPOINTMENT_REQUEST_UNAUTHORIZED";

    private readonly ApproveAppointmentRequestService approveAppointmentRequestService;
    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly DeclineAppointmentRequestService declineAppointmentRequestService;
    private readonly BusinessAppointmentRequestQueryService queryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessAppointmentRequestComposer(
        TenantBookingAuthorizationService authorizationService,
        BusinessAppointmentRequestQueryService queryService,
        ApproveAppointmentRequestService approveAppointmentRequestService,
        DeclineAppointmentRequestService declineAppointmentRequestService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.queryService = queryService;
        this.approveAppointmentRequestService = approveAppointmentRequestService;
        this.declineAppointmentRequestService = declineAppointmentRequestService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessAppointmentRequestListResult> GetPendingAsync(
        ClaimsPrincipal user,
        Guid? branchId,
        int? take,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessAppointmentRequestListResult.Failure(
                BusinessAppointmentRequestOutcome.Unauthorized,
                Unauthorized);
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessAppointmentRequestListResult.Failure(
                BusinessAppointmentRequestOutcome.BadRequest,
                MissingTenantContext);
        }

        if (!await authorizationService.CanManageAppointmentRequestsAsync(
            tenantId,
            userAccountId,
            branchId,
            cancellationToken))
        {
            return BusinessAppointmentRequestListResult.Failure(
                BusinessAppointmentRequestOutcome.Forbidden,
                Forbidden);
        }

        IReadOnlyCollection<BusinessAppointmentRequestListItemView> requests =
            await queryService.GetPendingAsync(
                branchId,
                take ?? 50,
                cancellationToken);

        return BusinessAppointmentRequestListResult.Success(
            requests
                .Select(entity => new BusinessAppointmentRequestResponse(
                    entity.Id,
                    entity.CustomerUserAccountId,
                    entity.BranchId,
                    entity.StaffMemberId,
                    entity.ResourceId,
                    entity.RequestedStartUtc,
                    entity.RequestedEndUtc,
                    entity.ExpiresAtUtc,
                    entity.Status,
                    entity.Lines
                        .Select(line => new BusinessAppointmentRequestLineResponse(
                            line.ServiceVariantId,
                            line.ServiceNameSnapshot,
                            line.DurationMinutes,
                            line.PriceAmount,
                            line.CurrencyCode))
                        .ToArray()))
                .ToArray());
    }

    public async Task<BusinessAppointmentRequestDecisionResult> ApproveAsync(
        ClaimsPrincipal user,
        Guid appointmentRequestId,
        CancellationToken cancellationToken = default)
    {
        return await DecideAsync(
            user,
            appointmentRequestId,
            "Approved",
            approveAppointmentRequestService.ApproveAsync,
            cancellationToken);
    }

    public async Task<BusinessAppointmentRequestDecisionResult> DeclineAsync(
        ClaimsPrincipal user,
        Guid appointmentRequestId,
        CancellationToken cancellationToken = default)
    {
        return await DecideAsync(
            user,
            appointmentRequestId,
            "Declined",
            declineAppointmentRequestService.DeclineAsync,
            cancellationToken);
    }

    private async Task<BusinessAppointmentRequestDecisionResult> DecideAsync(
        ClaimsPrincipal user,
        Guid appointmentRequestId,
        string successStatus,
        Func<Guid, Guid, CancellationToken, Task<AppointmentRequestDecisionResult>> decision,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessAppointmentRequestDecisionResult.Failure(
                BusinessAppointmentRequestOutcome.Unauthorized,
                Unauthorized);
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessAppointmentRequestDecisionResult.Failure(
                BusinessAppointmentRequestOutcome.BadRequest,
                MissingTenantContext);
        }

        if (!await authorizationService.HasAppointmentRequestManagementMembershipAsync(
            tenantId,
            userAccountId,
            cancellationToken))
        {
            return BusinessAppointmentRequestDecisionResult.Failure(
                BusinessAppointmentRequestOutcome.Forbidden,
                Forbidden);
        }

        BusinessAppointmentRequestAuthorizationContext? authorizationContext =
            await queryService.GetAuthorizationContextAsync(
                appointmentRequestId,
                cancellationToken);

        if (authorizationContext is null)
        {
            return BusinessAppointmentRequestDecisionResult.Failure(
                BusinessAppointmentRequestOutcome.NotFound,
                NotFound);
        }

        if (!await authorizationService.CanManageAppointmentRequestsAsync(
            tenantId,
            userAccountId,
            authorizationContext.BranchId,
            cancellationToken))
        {
            return BusinessAppointmentRequestDecisionResult.Failure(
                BusinessAppointmentRequestOutcome.Forbidden,
                Forbidden);
        }

        AppointmentRequestDecisionResult decisionResult =
            await decision(appointmentRequestId, userAccountId, cancellationToken);

        if (!decisionResult.Succeeded)
        {
            return MapDecisionFailure(decisionResult.ErrorCode ?? NotFound);
        }

        return BusinessAppointmentRequestDecisionResult.Success(
            new BusinessAppointmentRequestDecisionResponse(
                decisionResult.AppointmentId,
                decisionResult.AffectedRequests,
                successStatus));
    }

    private static BusinessAppointmentRequestDecisionResult MapDecisionFailure(string errorCode)
    {
        BusinessAppointmentRequestOutcome outcome = errorCode switch
        {
            "APPOINTMENT_REQUEST_NOT_FOUND" => BusinessAppointmentRequestOutcome.NotFound,
            "APPOINTMENT_CONFLICT" or "APPOINTMENT_REQUEST_ALREADY_CLOSED" => BusinessAppointmentRequestOutcome.Conflict,
            "APPOINTMENT_REQUEST_EXPIRED" => BusinessAppointmentRequestOutcome.Unprocessable,
            "MISSING_TENANT_CONTEXT" => BusinessAppointmentRequestOutcome.BadRequest,
            _ => BusinessAppointmentRequestOutcome.Unprocessable,
        };

        return BusinessAppointmentRequestDecisionResult.Failure(outcome, errorCode);
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
