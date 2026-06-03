using System.Security.Claims;
using RezSaaS.Api.Idempotency;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Identity.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessAppointmentRequestComposer
{
    private const string Forbidden = "BUSINESS_APPOINTMENT_REQUEST_FORBIDDEN";
    private const string InvalidStatus = "BUSINESS_APPOINTMENT_REQUEST_INVALID_STATUS";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "BUSINESS_APPOINTMENT_REQUEST_NOT_FOUND";
    private const string Unauthorized = "BUSINESS_APPOINTMENT_REQUEST_UNAUTHORIZED";

    private readonly ApproveAppointmentRequestService approveAppointmentRequestService;
    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly DeclineAppointmentRequestService declineAppointmentRequestService;
    private readonly CustomerAccountLookupService customerAccountLookupService;
    private readonly BusinessAppointmentRequestQueryService queryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessAppointmentRequestComposer(
        TenantBookingAuthorizationService authorizationService,
        BusinessAppointmentRequestQueryService queryService,
        ApproveAppointmentRequestService approveAppointmentRequestService,
        DeclineAppointmentRequestService declineAppointmentRequestService,
        CustomerAccountLookupService customerAccountLookupService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.queryService = queryService;
        this.approveAppointmentRequestService = approveAppointmentRequestService;
        this.declineAppointmentRequestService = declineAppointmentRequestService;
        this.customerAccountLookupService = customerAccountLookupService;
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
            await ToResponsesAsync(requests, cancellationToken));
    }

    public async Task<BusinessAppointmentRequestListResult> GetAsync(
        ClaimsPrincipal user,
        Guid? branchId,
        string? status,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
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

        if (!AppointmentRequestStatusFilter.IsValidOrEmpty(status))
        {
            return BusinessAppointmentRequestListResult.Failure(
                BusinessAppointmentRequestOutcome.BadRequest,
                InvalidStatus);
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
            await queryService.GetAsync(
                new BusinessAppointmentRequestQuery(
                    branchId,
                    status,
                    fromUtc,
                    toUtc,
                    take ?? 50),
                cancellationToken);

        return BusinessAppointmentRequestListResult.Success(
            await ToResponsesAsync(requests, cancellationToken));
    }

    public async Task<BusinessAppointmentRequestListResult> GetByIdAsync(
        ClaimsPrincipal user,
        Guid appointmentRequestId,
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

        BusinessAppointmentRequestAuthorizationContext? authorizationContext =
            await queryService.GetAuthorizationContextAsync(
                appointmentRequestId,
                cancellationToken);

        if (authorizationContext is null)
        {
            return BusinessAppointmentRequestListResult.Failure(
                BusinessAppointmentRequestOutcome.NotFound,
                NotFound);
        }

        if (!await authorizationService.CanManageAppointmentRequestsAsync(
            tenantId,
            userAccountId,
            authorizationContext.BranchId,
            cancellationToken))
        {
            return BusinessAppointmentRequestListResult.Failure(
                BusinessAppointmentRequestOutcome.Forbidden,
                Forbidden);
        }

        BusinessAppointmentRequestListItemView? request =
            await queryService.GetByIdAsync(
                appointmentRequestId,
                cancellationToken);

        if (request is null)
        {
            return BusinessAppointmentRequestListResult.Failure(
                BusinessAppointmentRequestOutcome.NotFound,
                NotFound);
        }

        return BusinessAppointmentRequestListResult.Success(
            await ToResponsesAsync([request], cancellationToken));
    }

    public async Task<BusinessAppointmentRequestDecisionResult> ApproveAsync(
        ClaimsPrincipal user,
        Guid appointmentRequestId,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await DecideAsync(
            user,
            appointmentRequestId,
            "Approved",
            idempotencyKey,
            "business.appointment-request.approve",
            (id, actorId, token, idempotency) => approveAppointmentRequestService.ApproveAsync(
                id,
                actorId,
                idempotency,
                token),
            cancellationToken);
    }

    public async Task<BusinessAppointmentRequestDecisionResult> DeclineAsync(
        ClaimsPrincipal user,
        Guid appointmentRequestId,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await DecideAsync(
            user,
            appointmentRequestId,
            "Declined",
            idempotencyKey,
            "business.appointment-request.decline",
            (id, actorId, token, idempotency) => declineAppointmentRequestService.DeclineAsync(
                id,
                actorId,
                idempotency,
                token),
            cancellationToken);
    }

    private async Task<BusinessAppointmentRequestDecisionResult> DecideAsync(
        ClaimsPrincipal user,
        Guid appointmentRequestId,
        string successStatus,
        string? idempotencyKey,
        string operation,
        Func<Guid, Guid, CancellationToken, BookingIdempotencyContext?, Task<AppointmentRequestDecisionResult>> decision,
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

        if (!BookingIdempotencyContextFactory.TryCreate(
            idempotencyKey,
            CreateDecisionIdempotencyMaterial(tenantId, appointmentRequestId, operation),
            out BookingIdempotencyContext? idempotency,
            out string? idempotencyErrorCode))
        {
            return BusinessAppointmentRequestDecisionResult.Failure(
                BusinessAppointmentRequestOutcome.BadRequest,
                idempotencyErrorCode!);
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
            await decision(appointmentRequestId, userAccountId, cancellationToken, idempotency);

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
            "APPOINTMENT_CONFLICT" or "APPOINTMENT_REQUEST_ALREADY_CLOSED" or "IDEMPOTENCY_KEY_REUSED" =>
                BusinessAppointmentRequestOutcome.Conflict,
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

    private async Task<IReadOnlyCollection<BusinessAppointmentRequestResponse>> ToResponsesAsync(
        IReadOnlyCollection<BusinessAppointmentRequestListItemView> requests,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, CustomerAccountMaskView> customers =
            await customerAccountLookupService.GetMaskedProfilesAsync(
                requests.Select(entity => entity.CustomerUserAccountId).ToArray(),
                cancellationToken);

        return requests
            .Select(entity =>
            {
                customers.TryGetValue(
                    entity.CustomerUserAccountId,
                    out CustomerAccountMaskView? customer);

                return new BusinessAppointmentRequestResponse(
                    entity.Id,
                    entity.CustomerUserAccountId,
                    new BusinessAppointmentRequestCustomerResponse(
                        entity.CustomerUserAccountId,
                        customer?.MaskedEmail ?? string.Empty,
                        customer?.MaskedPhone ?? string.Empty),
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
                        .ToArray());
            })
            .ToArray();
    }

    private static string CreateDecisionIdempotencyMaterial(
        Guid tenantId,
        Guid appointmentRequestId,
        string operation)
    {
        return string.Join(
            ';',
            $"operation={operation}",
            $"tenant={tenantId:D}",
            $"appointmentRequestId={appointmentRequestId:D}");
    }
}
