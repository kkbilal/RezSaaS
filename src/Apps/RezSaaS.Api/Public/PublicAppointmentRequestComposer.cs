using System.Security.Claims;
using Microsoft.Extensions.Options;
using RezSaaS.Api.Idempotency;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Application;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Catalog.Application;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Resources.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.PublicApi;

public sealed class PublicAppointmentRequestComposer
{
    private const string InvalidRequest = "PUBLIC_APPOINTMENT_REQUEST_INVALID";
    private const string InvalidStatus = "PUBLIC_APPOINTMENT_REQUEST_INVALID_STATUS";
    private const string MultipleResourceTypesNotSupported = "MULTIPLE_RESOURCE_TYPES_NOT_SUPPORTED";
    private const string NotFound = "PUBLIC_APPOINTMENT_REQUEST_TARGET_NOT_FOUND";
    private const string SlotUnavailable = "PUBLIC_APPOINTMENT_REQUEST_SLOT_UNAVAILABLE";
    private const string Unauthorized = "PUBLIC_APPOINTMENT_REQUEST_UNAUTHORIZED";

    private static readonly HashSet<string> TooManyRequestErrors =
    [
        "BOOKING_DAILY_LIMIT_EXCEEDED",
        "BOOKING_PENDING_LIMIT_EXCEEDED",
    ];

    private readonly AvailabilityQueryService availabilityQueryService;
    private readonly PublicBusinessDirectoryService businessDirectoryService;
    private readonly CancelAppointmentRequestService cancelAppointmentRequestService;
    private readonly ConfirmedAppointmentQueryService confirmedAppointmentQueryService;
    private readonly CreateAppointmentRequestService createAppointmentRequestService;
    private readonly CustomerAppointmentRequestQueryService customerAppointmentRequestQueryService;
    private readonly PublicCatalogSchedulingService catalogSchedulingService;
    private readonly PublicResourceAvailabilityQueryService resourceAvailabilityQueryService;
    private readonly IOptions<PublicSlotSearchOptions> slotSearchOptions;
    private readonly TenantLifecycleQueryService tenantLifecycleQueryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicAppointmentRequestComposer(
        PublicBusinessDirectoryService businessDirectoryService,
        PublicCatalogSchedulingService catalogSchedulingService,
        AvailabilityQueryService availabilityQueryService,
        PublicResourceAvailabilityQueryService resourceAvailabilityQueryService,
        ConfirmedAppointmentQueryService confirmedAppointmentQueryService,
        CreateAppointmentRequestService createAppointmentRequestService,
        CustomerAppointmentRequestQueryService customerAppointmentRequestQueryService,
        CancelAppointmentRequestService cancelAppointmentRequestService,
        TenantLifecycleQueryService tenantLifecycleQueryService,
        IOptions<PublicSlotSearchOptions> slotSearchOptions,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.businessDirectoryService = businessDirectoryService;
        this.catalogSchedulingService = catalogSchedulingService;
        this.availabilityQueryService = availabilityQueryService;
        this.resourceAvailabilityQueryService = resourceAvailabilityQueryService;
        this.confirmedAppointmentQueryService = confirmedAppointmentQueryService;
        this.createAppointmentRequestService = createAppointmentRequestService;
        this.customerAppointmentRequestQueryService = customerAppointmentRequestQueryService;
        this.cancelAppointmentRequestService = cancelAppointmentRequestService;
        this.tenantLifecycleQueryService = tenantLifecycleQueryService;
        this.slotSearchOptions = slotSearchOptions;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<PublicAppointmentRequestCreateResult> CreateAsync(
        string businessSlug,
        PublicAppointmentRequestCreateRequest request,
        string? idempotencyKey,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid customerUserAccountId))
        {
            return PublicAppointmentRequestCreateResult.Failure(
                PublicAppointmentRequestCreateOutcome.Unauthorized,
                Unauthorized);
        }

        if (!IsRequestShapeValid(request))
        {
            return PublicAppointmentRequestCreateResult.Failure(
                PublicAppointmentRequestCreateOutcome.BadRequest,
                InvalidRequest);
        }

        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(
                businessSlug,
                cancellationToken);

        if (business is null
            || !await tenantLifecycleQueryService.IsActiveAsync(
                business.TenantId,
                cancellationToken))
        {
            return PublicAppointmentRequestCreateResult.Failure(
                PublicAppointmentRequestCreateOutcome.NotFound,
                NotFound);
        }

        PublicBusinessBranchContext? branch = business.Branches
            .SingleOrDefault(entity =>
                string.Equals(entity.Slug, request.BranchSlug, StringComparison.OrdinalIgnoreCase));

        if (branch is null)
        {
            return PublicAppointmentRequestCreateResult.Failure(
                PublicAppointmentRequestCreateOutcome.NotFound,
                NotFound);
        }

        PublicStaffMemberView? selectedStaff = request.StaffMemberId is null
            ? null
            : branch.StaffMembers.SingleOrDefault(entity => entity.Id == request.StaffMemberId);

        if (request.StaffMemberId is not null && selectedStaff is null)
        {
            return PublicAppointmentRequestCreateResult.Failure(
                PublicAppointmentRequestCreateOutcome.NotFound,
                NotFound);
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            if (!BookingIdempotencyContextFactory.TryCreate(
                idempotencyKey,
                CreateIdempotencyMaterial(businessSlug, request),
                out BookingIdempotencyContext? idempotency,
                out string? idempotencyErrorCode))
            {
                return PublicAppointmentRequestCreateResult.Failure(
                    PublicAppointmentRequestCreateOutcome.BadRequest,
                    idempotencyErrorCode!);
            }

            Guid[] serviceVariantIds = request.ServiceVariantIds
                .Distinct()
                .ToArray();
            IReadOnlyCollection<PublicServiceVariantSchedulingView> variants =
                await catalogSchedulingService.GetVariantsAsync(
                    serviceVariantIds,
                    cancellationToken);

            if (variants.Count != serviceVariantIds.Length)
            {
                return PublicAppointmentRequestCreateResult.Failure(
                    PublicAppointmentRequestCreateOutcome.NotFound,
                    NotFound);
            }

            Guid[] requiredSkillIds = variants
                .SelectMany(entity => entity.RequiredSkillIds)
                .Distinct()
                .ToArray();

            PublicStaffMemberView[] staffCandidates = GetStaffCandidates(
                branch,
                request.StaffMemberId,
                requiredSkillIds);

            if (staffCandidates.Length == 0)
            {
                return PublicAppointmentRequestCreateResult.Failure(
                    PublicAppointmentRequestCreateOutcome.Conflict,
                    SlotUnavailable);
            }

            Guid[] requiredResourceTypeIds = variants
                .Select(entity => entity.RequiredResourceTypeId)
                .OfType<Guid>()
                .Distinct()
                .ToArray();

            if (requiredResourceTypeIds.Length > 1)
            {
                return PublicAppointmentRequestCreateResult.Failure(
                    PublicAppointmentRequestCreateOutcome.BadRequest,
                    MultipleResourceTypesNotSupported);
            }

            Guid? requiredResourceTypeId = requiredResourceTypeIds.Length == 0
                ? null
                : requiredResourceTypeIds[0];
            PublicResourceCandidateView[] resourceCandidates =
                (await resourceAvailabilityQueryService.GetActiveResourcesAsync(
                    branch.Id,
                    requiredResourceTypeId,
                    cancellationToken))
                .ToArray();

            int durationMinutes = variants.Sum(entity => entity.DurationMinutes);
            DateTimeOffset endUtc = request.StartUtc.AddMinutes(durationMinutes);
            PublicAppointmentAssignment? assignment = await FindAvailableAssignmentAsync(
                branch,
                staffCandidates,
                resourceCandidates,
                request.StartUtc,
                endUtc,
                cancellationToken);

            if (assignment is null)
            {
                return PublicAppointmentRequestCreateResult.Failure(
                    PublicAppointmentRequestCreateOutcome.Conflict,
                    SlotUnavailable);
            }

            Dictionary<Guid, PublicServiceVariantSchedulingView> variantsById =
                variants.ToDictionary(entity => entity.Id);
            AppointmentRequestLineInput[] lines = serviceVariantIds
                .Select(serviceVariantId =>
                {
                    PublicServiceVariantSchedulingView variant = variantsById[serviceVariantId];
                    return new AppointmentRequestLineInput(
                        variant.Id,
                        variant.ServiceName,
                        variant.DurationMinutes,
                        variant.PriceAmount,
                        variant.CurrencyCode);
                })
                .ToArray();
            CreateAppointmentRequestResult result =
                await createAppointmentRequestService.CreateAsync(
                    new CreateAppointmentRequestCommand(
                        customerUserAccountId,
                        branch.Id,
                        assignment.StaffMemberId,
                        assignment.ResourceId,
                        request.StartUtc,
                        endUtc,
                        lines,
                        Idempotency: idempotency),
                    cancellationToken);

            if (!result.Succeeded)
            {
                return MapCreateFailure(result.ErrorCode ?? InvalidRequest);
            }

            return result.IsReplay
                ? PublicAppointmentRequestCreateResult.Replayed(
                    result.AppointmentRequestId!.Value,
                    result.ExpiresAtUtc!.Value,
                    result.Status ?? "PendingApproval")
                : PublicAppointmentRequestCreateResult.Created(
                    result.AppointmentRequestId!.Value,
                    result.ExpiresAtUtc!.Value,
                    result.Status ?? "PendingApproval");
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }

    public async Task<PublicAppointmentRequestAccessResult> GetOwnAsync(
        string businessSlug,
        string? status,
        int? take,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid customerUserAccountId))
        {
            return PublicAppointmentRequestAccessResult.Failure(
                PublicAppointmentRequestAccessOutcome.Unauthorized,
                Unauthorized);
        }

        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(
                businessSlug,
                cancellationToken);

        if (business is null)
        {
            return PublicAppointmentRequestAccessResult.Failure(
                PublicAppointmentRequestAccessOutcome.NotFound,
                NotFound);
        }

        if (!AppointmentRequestStatusFilter.IsValidOrEmpty(status))
        {
            return PublicAppointmentRequestAccessResult.Failure(
                PublicAppointmentRequestAccessOutcome.BadRequest,
                InvalidStatus);
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            IReadOnlyCollection<CustomerAppointmentRequestView> requests =
                await customerAppointmentRequestQueryService.GetOwnAsync(
                    customerUserAccountId,
                    business.Branches.Select(entity => entity.Id).ToArray(),
                    status,
                    take ?? 50,
                    cancellationToken);

            return PublicAppointmentRequestAccessResult.Success(
                requests
                    .Select(request => ToResponse(business, request))
                    .ToArray());
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }

    public async Task<PublicAppointmentRequestAccessResult> GetOwnByIdAsync(
        string businessSlug,
        Guid appointmentRequestId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid customerUserAccountId))
        {
            return PublicAppointmentRequestAccessResult.Failure(
                PublicAppointmentRequestAccessOutcome.Unauthorized,
                Unauthorized);
        }

        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(
                businessSlug,
                cancellationToken);

        if (business is null)
        {
            return PublicAppointmentRequestAccessResult.Failure(
                PublicAppointmentRequestAccessOutcome.NotFound,
                NotFound);
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            CustomerAppointmentRequestView? request =
                await customerAppointmentRequestQueryService.GetOwnByIdAsync(
                    customerUserAccountId,
                    appointmentRequestId,
                    business.Branches.Select(entity => entity.Id).ToArray(),
                    cancellationToken);

            return request is null
                ? PublicAppointmentRequestAccessResult.Failure(
                    PublicAppointmentRequestAccessOutcome.NotFound,
                    NotFound)
                : PublicAppointmentRequestAccessResult.Success(ToResponse(business, request));
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }

    public async Task<PublicAppointmentRequestAccessResult> CancelOwnAsync(
        string businessSlug,
        Guid appointmentRequestId,
        string? idempotencyKey,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid customerUserAccountId))
        {
            return PublicAppointmentRequestAccessResult.Failure(
                PublicAppointmentRequestAccessOutcome.Unauthorized,
                Unauthorized);
        }

        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(
                businessSlug,
                cancellationToken);

        if (business is null)
        {
            return PublicAppointmentRequestAccessResult.Failure(
                PublicAppointmentRequestAccessOutcome.NotFound,
                NotFound);
        }

        if (!BookingIdempotencyContextFactory.TryCreate(
            idempotencyKey,
            CreateCancelIdempotencyMaterial(businessSlug, appointmentRequestId),
            out BookingIdempotencyContext? idempotency,
            out string? idempotencyErrorCode))
        {
            return PublicAppointmentRequestAccessResult.Failure(
                PublicAppointmentRequestAccessOutcome.BadRequest,
                idempotencyErrorCode!);
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            CustomerAppointmentRequestView? existingRequest =
                await customerAppointmentRequestQueryService.GetOwnByIdAsync(
                    customerUserAccountId,
                    appointmentRequestId,
                    business.Branches.Select(entity => entity.Id).ToArray(),
                    cancellationToken);

            if (existingRequest is null)
            {
                return PublicAppointmentRequestAccessResult.Failure(
                    PublicAppointmentRequestAccessOutcome.NotFound,
                    NotFound);
            }

            AppointmentRequestDecisionResult result =
                await cancelAppointmentRequestService.CancelAsync(
                    appointmentRequestId,
                    customerUserAccountId,
                    idempotency,
                    cancellationToken);

            if (!result.Succeeded)
            {
                return MapAccessFailure(result.ErrorCode ?? InvalidRequest);
            }

            CustomerAppointmentRequestView? cancelledRequest =
                await customerAppointmentRequestQueryService.GetOwnByIdAsync(
                    customerUserAccountId,
                    appointmentRequestId,
                    business.Branches.Select(entity => entity.Id).ToArray(),
                    cancellationToken);

            return cancelledRequest is null
                ? PublicAppointmentRequestAccessResult.Failure(
                    PublicAppointmentRequestAccessOutcome.NotFound,
                    NotFound)
                : PublicAppointmentRequestAccessResult.Success(ToResponse(business, cancelledRequest));
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }

    private async Task<PublicAppointmentAssignment?> FindAvailableAssignmentAsync(
        PublicBusinessBranchContext branch,
        PublicStaffMemberView[] staffCandidates,
        PublicResourceCandidateView[] resourceCandidates,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        if (staffCandidates.Length == 0 || resourceCandidates.Length == 0)
        {
            return null;
        }

        if (!PublicTimeZoneResolver.TryFind(branch.TimeZoneId, out TimeZoneInfo? timeZoneInfo))
        {
            return null;
        }

        DateTime localStart = PublicTimeZoneResolver.ConvertUtcToLocal(startUtc, timeZoneInfo!);
        DateTime localEnd = PublicTimeZoneResolver.ConvertUtcToLocal(endUtc, timeZoneInfo!);
        DateOnly localDate = DateOnly.FromDateTime(localStart);

        if (DateOnly.FromDateTime(localEnd) != localDate)
        {
            return null;
        }

        DateTimeOffset dayStartUtc = PublicTimeZoneResolver.ConvertLocalToUtc(
            localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            timeZoneInfo!);
        DateTimeOffset dayEndUtc = PublicTimeZoneResolver.ConvertLocalToUtc(
            localDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
            timeZoneInfo!);
        AvailabilitySnapshot? availabilitySnapshot =
            await availabilityQueryService.GetBranchSnapshotAsync(
                branch.Id,
                dayStartUtc,
                dayEndUtc,
                staffCandidates.Select(entity => entity.Id).ToArray(),
                cancellationToken);

        if (availabilitySnapshot is null)
        {
            return null;
        }

        BranchWorkingHoursView? workingHours = availabilitySnapshot.WorkingHours
            .SingleOrDefault(entity => entity.DayOfWeek == localDate.DayOfWeek);

        if (workingHours is null || workingHours.IsClosed)
        {
            return null;
        }

        DateTime opensAtLocal = localDate.ToDateTime(workingHours.OpensAt, DateTimeKind.Unspecified);
        DateTime closesAtLocal = localDate.ToDateTime(workingHours.ClosesAt, DateTimeKind.Unspecified);

        if (localStart < opensAtLocal || localEnd > closesAtLocal)
        {
            return null;
        }

        if (!IsAlignedToSlotGrid(
            localStart,
            opensAtLocal,
            branch.SlotIntervalMinutes ?? slotSearchOptions.Value.SlotIntervalMinutes))
        {
            return null;
        }

        IReadOnlyCollection<PublicResourceBlockView> resourceBlocks =
            await resourceAvailabilityQueryService.GetResourceBlocksAsync(
                resourceCandidates.Select(entity => entity.Id).ToArray(),
                startUtc,
                endUtc,
                cancellationToken);

        IReadOnlyCollection<ConfirmedAppointmentBusyTimeView> confirmedBusyTimes =
            await confirmedAppointmentQueryService.GetBusyTimesAsync(
                branch.Id,
                startUtc,
                endUtc,
                cancellationToken);

        foreach (PublicStaffMemberView staff in staffCandidates)
        {
            if (availabilitySnapshot.StaffUnavailableTimes.Any(entity =>
                entity.StaffMemberId == staff.Id
                && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc))
                || confirmedBusyTimes.Any(entity =>
                    entity.StaffMemberId == staff.Id
                    && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc)))
            {
                continue;
            }

            PublicResourceCandidateView? resource = resourceCandidates
                .FirstOrDefault(entity =>
                    !resourceBlocks.Any(block =>
                        block.ResourceId == entity.Id
                        && Overlaps(startUtc, endUtc, block.StartUtc, block.EndUtc))
                    && !confirmedBusyTimes.Any(busy =>
                        busy.ResourceId == entity.Id
                        && Overlaps(startUtc, endUtc, busy.StartUtc, busy.EndUtc)));

            if (resource is not null)
            {
                return new PublicAppointmentAssignment(staff.Id, resource.Id);
            }
        }

        return null;
    }

    private static bool IsRequestShapeValid(PublicAppointmentRequestCreateRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.BranchSlug)
            && request.ServiceVariantIds.Count > 0
            && request.ServiceVariantIds.All(entity => entity != Guid.Empty)
            && request.StaffMemberId != Guid.Empty
            && request.StartUtc != default;
    }

    private static PublicStaffMemberView[] GetStaffCandidates(
        PublicBusinessBranchContext branch,
        Guid? staffMemberId,
        Guid[] requiredSkillIds)
    {
        IEnumerable<PublicStaffMemberView> staffMembers = branch.StaffMembers;

        if (staffMemberId is not null)
        {
            staffMembers = staffMembers.Where(entity => entity.Id == staffMemberId);
        }

        if (requiredSkillIds.Length > 0)
        {
            staffMembers = staffMembers.Where(entity =>
                requiredSkillIds.All(requiredSkillId => entity.SkillIds.Contains(requiredSkillId)));
        }

        return staffMembers.ToArray();
    }

    private static bool IsAlignedToSlotGrid(
        DateTime localStart,
        DateTime opensAtLocal,
        int slotIntervalMinutes)
    {
        if (slotIntervalMinutes <= 0)
        {
            return false;
        }

        TimeSpan slotOffset = localStart - opensAtLocal;

        return slotOffset >= TimeSpan.Zero
            && slotOffset.Ticks % TimeSpan.FromMinutes(slotIntervalMinutes).Ticks == 0;
    }

    private static PublicAppointmentRequestCreateResult MapCreateFailure(string errorCode)
    {
        if (TooManyRequestErrors.Contains(errorCode))
        {
            return PublicAppointmentRequestCreateResult.Failure(
                PublicAppointmentRequestCreateOutcome.TooManyRequests,
                errorCode);
        }

        PublicAppointmentRequestCreateOutcome outcome = errorCode switch
        {
            "INVALID_TIME_RANGE" or "APPOINTMENT_REQUEST_LINES_REQUIRED" => PublicAppointmentRequestCreateOutcome.BadRequest,
            "BOOKING_USER_SANCTIONED" => PublicAppointmentRequestCreateOutcome.Forbidden,
            "IDEMPOTENCY_KEY_REUSED" => PublicAppointmentRequestCreateOutcome.Conflict,
            "APPOINTMENT_REQUEST_TOO_SOON" => PublicAppointmentRequestCreateOutcome.Unprocessable,
            _ => PublicAppointmentRequestCreateOutcome.Unprocessable,
        };

        return PublicAppointmentRequestCreateResult.Failure(outcome, errorCode);
    }

    private static bool Overlaps(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        DateTimeOffset busyStartUtc,
        DateTimeOffset busyEndUtc)
    {
        return startUtc < busyEndUtc && endUtc > busyStartUtc;
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }

    private static PublicAppointmentRequestAccessResult MapAccessFailure(string errorCode)
    {
        PublicAppointmentRequestAccessOutcome outcome = errorCode switch
        {
            "APPOINTMENT_REQUEST_NOT_FOUND" => PublicAppointmentRequestAccessOutcome.NotFound,
            "APPOINTMENT_REQUEST_ALREADY_CLOSED" or "IDEMPOTENCY_KEY_REUSED" =>
                PublicAppointmentRequestAccessOutcome.Conflict,
            "MISSING_TENANT_CONTEXT" => PublicAppointmentRequestAccessOutcome.BadRequest,
            _ => PublicAppointmentRequestAccessOutcome.Unprocessable,
        };

        return PublicAppointmentRequestAccessResult.Failure(outcome, errorCode);
    }

    private static PublicAppointmentRequestResponse ToResponse(
        PublicBusinessCompositionContext business,
        CustomerAppointmentRequestView request)
    {
        PublicBusinessBranchContext branch = business.Branches
            .Single(entity => entity.Id == request.BranchId);

        return new PublicAppointmentRequestResponse(
            request.Id,
            business.Slug,
            branch.Slug,
            branch.DisplayName,
            request.StaffMemberId,
            request.RequestedStartUtc,
            request.RequestedEndUtc,
            request.ExpiresAtUtc,
            request.Status,
            request.Lines
                .Select(line => new PublicAppointmentRequestLineResponse(
                    line.ServiceVariantId,
                    line.ServiceNameSnapshot,
                    line.DurationMinutes,
                    line.PriceAmount,
                    line.CurrencyCode))
                .ToArray());
    }

    private static string CreateIdempotencyMaterial(
        string businessSlug,
        PublicAppointmentRequestCreateRequest request)
    {
        string serviceVariantIds = string.Join(
            ',',
            request.ServiceVariantIds
                .Distinct()
                .OrderBy(entity => entity)
                .Select(entity => entity.ToString("D")));

        return string.Join(
            ';',
            "operation=public.appointment-request.create",
            $"business={businessSlug.Trim().ToUpperInvariant()}",
            $"branch={request.BranchSlug.Trim().ToUpperInvariant()}",
            $"variants={serviceVariantIds}",
            $"staff={request.StaffMemberId?.ToString("D") ?? "ANY"}",
            $"startUtc={request.StartUtc.UtcDateTime:O}");
    }

    private static string CreateCancelIdempotencyMaterial(
        string businessSlug,
        Guid appointmentRequestId)
    {
        return string.Join(
            ';',
            "operation=public.appointment-request.cancel",
            $"business={businessSlug.Trim().ToUpperInvariant()}",
            $"appointmentRequestId={appointmentRequestId:D}");
    }

    private sealed record PublicAppointmentAssignment(
        Guid StaffMemberId,
        Guid ResourceId);
}
