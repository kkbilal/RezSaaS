using System.Security.Claims;
using System.Globalization;
using RezSaaS.Api.Idempotency;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Identity.Application;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Resources.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Business;

public sealed class BusinessAppointmentComposer
{
    private const string Forbidden = "BUSINESS_APPOINTMENT_FORBIDDEN";
    private const string InvalidStatus = "BUSINESS_APPOINTMENT_INVALID_STATUS";
    private const string InvalidTimeRange = "BUSINESS_APPOINTMENT_INVALID_TIME_RANGE";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "BUSINESS_APPOINTMENT_NOT_FOUND";
    private const string Unauthorized = "BUSINESS_APPOINTMENT_UNAUTHORIZED";

    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly CustomerAccountLookupService customerAccountLookupService;
    private readonly BusinessEntityLabelQueryService labelQueryService;
    private readonly BusinessAppointmentOperationService operationService;
    private readonly BusinessAppointmentQueryService queryService;
    private readonly ResourceLabelQueryService resourceLabelQueryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessAppointmentComposer(
        TenantBookingAuthorizationService authorizationService,
        BusinessAppointmentQueryService queryService,
        BusinessAppointmentOperationService operationService,
        CustomerAccountLookupService customerAccountLookupService,
        BusinessEntityLabelQueryService labelQueryService,
        ResourceLabelQueryService resourceLabelQueryService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.authorizationService = authorizationService;
        this.queryService = queryService;
        this.operationService = operationService;
        this.customerAccountLookupService = customerAccountLookupService;
        this.labelQueryService = labelQueryService;
        this.resourceLabelQueryService = resourceLabelQueryService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessAppointmentListResult> GetAsync(
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
            return BusinessAppointmentListResult.Failure(
                BusinessAppointmentOutcome.Unauthorized,
                Unauthorized);
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessAppointmentListResult.Failure(
                BusinessAppointmentOutcome.BadRequest,
                MissingTenantContext);
        }

        if (!AppointmentStatusFilter.IsValidOrEmpty(status))
        {
            return BusinessAppointmentListResult.Failure(
                BusinessAppointmentOutcome.BadRequest,
                InvalidStatus);
        }

        DateTimeOffset rangeStartUtc = fromUtc ?? DateTimeOffset.UtcNow.Date;
        DateTimeOffset rangeEndUtc = toUtc ?? rangeStartUtc.AddDays(7);

        if (rangeEndUtc <= rangeStartUtc)
        {
            return BusinessAppointmentListResult.Failure(
                BusinessAppointmentOutcome.BadRequest,
                InvalidTimeRange);
        }

        if (!await authorizationService.CanManageAppointmentRequestsAsync(
            tenantId,
            userAccountId,
            branchId,
            cancellationToken))
        {
            return BusinessAppointmentListResult.Failure(
                BusinessAppointmentOutcome.Forbidden,
                Forbidden);
        }

        IReadOnlyCollection<BusinessAppointmentListItemView> appointments =
            await queryService.GetAsync(
                new BusinessAppointmentQuery(
                    branchId,
                    status,
                    rangeStartUtc,
                    rangeEndUtc,
                    take ?? 100),
                cancellationToken);

        return BusinessAppointmentListResult.Success(
            await ToResponsesAsync(appointments, cancellationToken));
    }

    public async Task<BusinessAppointmentListResult> GetByIdAsync(
        ClaimsPrincipal user,
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        BusinessAppointmentOutcome authOutcome = await AuthorizeAppointmentAsync(
            user,
            appointmentId,
            cancellationToken);

        if (authOutcome != BusinessAppointmentOutcome.Success)
        {
            return BusinessAppointmentListResult.Failure(
                authOutcome,
                authOutcome == BusinessAppointmentOutcome.NotFound ? NotFound : Forbidden);
        }

        BusinessAppointmentListItemView? appointment =
            await queryService.GetByIdAsync(appointmentId, cancellationToken);

        if (appointment is null)
        {
            return BusinessAppointmentListResult.Failure(
                BusinessAppointmentOutcome.NotFound,
                NotFound);
        }

        return BusinessAppointmentListResult.Success(
            await ToResponsesAsync([appointment], cancellationToken));
    }

    public async Task<BusinessAppointmentOperationResult> CancelAsync(
        ClaimsPrincipal user,
        Guid appointmentId,
        BusinessAppointmentCancelRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            user,
            appointmentId,
            "business.appointment.cancel",
            idempotencyKey,
            ["reason", NormalizeMaterial(request.Reason)],
            (actorId, context, token) => operationService.CancelAsync(
                appointmentId,
                actorId,
                request.Reason,
                context,
                token),
            cancellationToken);
    }

    public async Task<BusinessAppointmentOperationResult> CompleteAsync(
        ClaimsPrincipal user,
        Guid appointmentId,
        BusinessAppointmentCompleteRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            user,
            appointmentId,
            "business.appointment.complete",
            idempotencyKey,
            ["note", NormalizeMaterial(request.Note)],
            (actorId, context, token) => operationService.CompleteAsync(
                appointmentId,
                actorId,
                request.Note,
                context,
                token),
            cancellationToken);
    }

    public async Task<BusinessAppointmentOperationResult> MarkNoShowAsync(
        ClaimsPrincipal user,
        Guid appointmentId,
        BusinessAppointmentNoShowRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            user,
            appointmentId,
            "business.appointment.no-show",
            idempotencyKey,
            ["reason", NormalizeMaterial(request.Reason)],
            (actorId, context, token) => operationService.MarkNoShowAsync(
                appointmentId,
                actorId,
                request.Reason,
                context,
                token),
            cancellationToken);
    }

    public async Task<BusinessAppointmentOperationResult> UpdateNoteAsync(
        ClaimsPrincipal user,
        Guid appointmentId,
        BusinessAppointmentNoteRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            user,
            appointmentId,
            "business.appointment.note",
            idempotencyKey,
            ["note", NormalizeMaterial(request.Note)],
            (actorId, context, token) => operationService.UpdateNoteAsync(
                appointmentId,
                actorId,
                request.Note,
                context,
                token),
            cancellationToken);
    }

    public async Task<BusinessAppointmentOperationResult> RebookAsync(
        ClaimsPrincipal user,
        Guid appointmentId,
        BusinessAppointmentRebookRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            user,
            appointmentId,
            "business.appointment.rebook",
            idempotencyKey,
            [
                "start", request.StartUtc.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                "end", request.EndUtc.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
                "staff", request.StaffMemberId?.ToString("D") ?? "existing",
                "resource", request.ResourceId?.ToString("D") ?? "existing",
                "reason", NormalizeMaterial(request.Reason),
            ],
            (actorId, context, token) => operationService.RebookAsync(
                appointmentId,
                actorId,
                request.StartUtc,
                request.EndUtc,
                request.StaffMemberId,
                request.ResourceId,
                request.Reason,
                context,
                token),
            cancellationToken);
    }

    private async Task<BusinessAppointmentOperationResult> ExecuteAsync(
        ClaimsPrincipal user,
        Guid appointmentId,
        string operation,
        string? idempotencyKey,
        string[] requestMaterialParts,
        Func<Guid, BookingIdempotencyContext?, CancellationToken, Task<AppointmentOperationResult>> execute,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessAppointmentOperationResult.Failure(
                BusinessAppointmentOutcome.Unauthorized,
                Unauthorized);
        }

        BusinessAppointmentOutcome authOutcome = await AuthorizeAppointmentAsync(
            user,
            appointmentId,
            cancellationToken);

        if (authOutcome != BusinessAppointmentOutcome.Success)
        {
            return BusinessAppointmentOperationResult.Failure(
                authOutcome,
                authOutcome == BusinessAppointmentOutcome.NotFound ? NotFound : Forbidden);
        }

        if (!BookingIdempotencyContextFactory.TryCreate(
            idempotencyKey,
            CreateIdempotencyMaterial(appointmentId, operation, requestMaterialParts),
            out BookingIdempotencyContext? idempotency,
            out string? idempotencyErrorCode))
        {
            return BusinessAppointmentOperationResult.Failure(
                BusinessAppointmentOutcome.BadRequest,
                idempotencyErrorCode!);
        }

        AppointmentOperationResult result = await execute(
            userAccountId,
            idempotency,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BusinessAppointmentOperationResult.Failure(
                ToOutcome(result.ErrorCode),
                result.ErrorCode ?? "BUSINESS_APPOINTMENT_OPERATION_FAILED");
        }

        return BusinessAppointmentOperationResult.Success(
            new BusinessAppointmentOperationResponse(
                result.AppointmentId!.Value,
                result.RelatedAppointmentId,
                result.Status!));
    }

    private async Task<BusinessAppointmentOutcome> AuthorizeAppointmentAsync(
        ClaimsPrincipal user,
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessAppointmentOutcome.Unauthorized;
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessAppointmentOutcome.BadRequest;
        }

        BusinessAppointmentAuthorizationContext? authorizationContext =
            await queryService.GetAuthorizationContextAsync(appointmentId, cancellationToken);

        if (authorizationContext is null)
        {
            return BusinessAppointmentOutcome.NotFound;
        }

        return await authorizationService.CanManageAppointmentRequestsAsync(
            tenantId,
            userAccountId,
            authorizationContext.BranchId,
            cancellationToken)
            ? BusinessAppointmentOutcome.Success
            : BusinessAppointmentOutcome.Forbidden;
    }

    private async Task<IReadOnlyCollection<BusinessAppointmentResponse>> ToResponsesAsync(
        IReadOnlyCollection<BusinessAppointmentListItemView> appointments,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, CustomerAccountMaskView> customers =
            await customerAccountLookupService.GetMaskedProfilesAsync(
                appointments.Select(entity => entity.CustomerUserAccountId).ToArray(),
                cancellationToken);
        IReadOnlyDictionary<Guid, BranchLabelView> branches =
            await labelQueryService.GetBranchLabelsAsync(
                appointments.Select(entity => entity.BranchId).ToArray(),
                cancellationToken);
        IReadOnlyDictionary<Guid, StaffMemberLabelView> staffMembers =
            await labelQueryService.GetStaffMemberLabelsAsync(
                appointments.Select(entity => entity.StaffMemberId).ToArray(),
                cancellationToken);
        IReadOnlyDictionary<Guid, ResourceLabelView> resources =
            await resourceLabelQueryService.GetResourceLabelsAsync(
                appointments.Select(entity => entity.ResourceId).ToArray(),
                cancellationToken);

        return appointments
            .Select(entity =>
            {
                customers.TryGetValue(entity.CustomerUserAccountId, out CustomerAccountMaskView? customer);
                branches.TryGetValue(entity.BranchId, out BranchLabelView? branch);
                staffMembers.TryGetValue(entity.StaffMemberId, out StaffMemberLabelView? staff);
                resources.TryGetValue(entity.ResourceId, out ResourceLabelView? resource);

                return new BusinessAppointmentResponse(
                    entity.Id,
                    entity.AppointmentRequestId,
                    new BusinessAppointmentRequestCustomerResponse(
                        entity.CustomerUserAccountId,
                        customer?.MaskedEmail ?? string.Empty,
                        customer?.MaskedPhone ?? string.Empty),
                    entity.BranchId,
                    branch?.DisplayName ?? string.Empty,
                    branch?.TimeZoneId ?? string.Empty,
                    entity.StaffMemberId,
                    staff?.DisplayName ?? string.Empty,
                    entity.ResourceId,
                    resource?.DisplayName ?? string.Empty,
                    entity.StartUtc,
                    entity.EndUtc,
                    entity.Status,
                    entity.BusinessNote,
                    entity.CancelledAtUtc,
                    entity.CancellationReason,
                    entity.CompletedAtUtc,
                    entity.CompletionNote,
                    entity.NoShowAtUtc,
                    entity.NoShowReason,
                    entity.RebookedFromAppointmentId,
                    entity.RebookedToAppointmentId,
                    entity.RebookedAtUtc,
                    entity.RebookReason,
                    entity.Lines
                        .Select(line => new BusinessAppointmentLineResponse(
                            line.ServiceVariantId,
                            line.ServiceNameSnapshot,
                            line.DurationMinutes,
                            line.PriceAmount,
                            line.CurrencyCode))
                        .ToArray());
            })
            .ToArray();
    }

    private static BusinessAppointmentOutcome ToOutcome(string? errorCode)
    {
        return errorCode switch
        {
            "APPOINTMENT_NOT_FOUND" => BusinessAppointmentOutcome.NotFound,
            "APPOINTMENT_ALREADY_CLOSED" => BusinessAppointmentOutcome.Conflict,
            "APPOINTMENT_COMPLETE_TOO_EARLY" => BusinessAppointmentOutcome.Conflict,
            "APPOINTMENT_CONFLICT" => BusinessAppointmentOutcome.Conflict,
            "APPOINTMENT_INVALID_TIME_RANGE" => BusinessAppointmentOutcome.BadRequest,
            "APPOINTMENT_NO_SHOW_TOO_EARLY" => BusinessAppointmentOutcome.Conflict,
            "IDEMPOTENCY_KEY_REUSED" => BusinessAppointmentOutcome.Conflict,
            "MISSING_TENANT_CONTEXT" => BusinessAppointmentOutcome.BadRequest,
            _ => BusinessAppointmentOutcome.BadRequest,
        };
    }

    private static string CreateIdempotencyMaterial(
        Guid appointmentId,
        string operation,
        IReadOnlyCollection<string> parts)
    {
        return string.Join(
            "|",
            new[]
                {
                    $"appointment={appointmentId:D}",
                    $"operation={operation}",
                }
                .Concat(parts));
    }

    private static string NormalizeMaterial(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(rawUserId, out userAccountId);
    }
}
