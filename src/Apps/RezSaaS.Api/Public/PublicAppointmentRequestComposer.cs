using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Application;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Catalog.Application;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Resources.Application;

namespace RezSaaS.Api.PublicApi;

public sealed class PublicAppointmentRequestComposer
{
    private const string InvalidRequest = "PUBLIC_APPOINTMENT_REQUEST_INVALID";
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
    private readonly ConfirmedAppointmentQueryService confirmedAppointmentQueryService;
    private readonly CreateAppointmentRequestService createAppointmentRequestService;
    private readonly PublicCatalogSchedulingService catalogSchedulingService;
    private readonly PublicResourceAvailabilityQueryService resourceAvailabilityQueryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicAppointmentRequestComposer(
        PublicBusinessDirectoryService businessDirectoryService,
        PublicCatalogSchedulingService catalogSchedulingService,
        AvailabilityQueryService availabilityQueryService,
        PublicResourceAvailabilityQueryService resourceAvailabilityQueryService,
        ConfirmedAppointmentQueryService confirmedAppointmentQueryService,
        CreateAppointmentRequestService createAppointmentRequestService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.businessDirectoryService = businessDirectoryService;
        this.catalogSchedulingService = catalogSchedulingService;
        this.availabilityQueryService = availabilityQueryService;
        this.resourceAvailabilityQueryService = resourceAvailabilityQueryService;
        this.confirmedAppointmentQueryService = confirmedAppointmentQueryService;
        this.createAppointmentRequestService = createAppointmentRequestService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<PublicAppointmentRequestCreateResult> CreateAsync(
        string businessSlug,
        PublicAppointmentRequestCreateRequest request,
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

        if (business is null)
        {
            return PublicAppointmentRequestCreateResult.Failure(
                PublicAppointmentRequestCreateOutcome.NotFound,
                NotFound);
        }

        PublicBusinessBranchContext? branch = business.Branches
            .SingleOrDefault(entity =>
                string.Equals(entity.Slug, request.BranchSlug, StringComparison.OrdinalIgnoreCase));

        if (branch is null || branch.StaffMembers.All(entity => entity.Id != request.StaffMemberId))
        {
            return PublicAppointmentRequestCreateResult.Failure(
                PublicAppointmentRequestCreateOutcome.NotFound,
                NotFound);
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
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
            IReadOnlyCollection<PublicResourceCandidateView> resourceCandidates =
                await resourceAvailabilityQueryService.GetActiveResourcesAsync(
                    branch.Id,
                    requiredResourceTypeId,
                    cancellationToken);
            PublicResourceCandidateView? selectedResource = resourceCandidates
                .SingleOrDefault(entity => entity.Id == request.ResourceId);

            if (selectedResource is null)
            {
                return PublicAppointmentRequestCreateResult.Failure(
                    PublicAppointmentRequestCreateOutcome.NotFound,
                    NotFound);
            }

            int durationMinutes = variants.Sum(entity => entity.DurationMinutes);
            DateTimeOffset endUtc = request.StartUtc.AddMinutes(durationMinutes);

            if (!await IsSlotAvailableAsync(
                branch,
                request.StaffMemberId,
                selectedResource.Id,
                request.StartUtc,
                endUtc,
                cancellationToken))
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
                        request.StaffMemberId,
                        selectedResource.Id,
                        request.StartUtc,
                        endUtc,
                        lines),
                    cancellationToken);

            if (!result.Succeeded)
            {
                return MapCreateFailure(result.ErrorCode ?? InvalidRequest);
            }

            return PublicAppointmentRequestCreateResult.Created(
                result.AppointmentRequestId!.Value,
                result.ExpiresAtUtc!.Value);
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }

    private async Task<bool> IsSlotAvailableAsync(
        PublicBusinessBranchContext branch,
        Guid staffMemberId,
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        if (!PublicTimeZoneResolver.TryFind(branch.TimeZoneId, out TimeZoneInfo? timeZoneInfo))
        {
            return false;
        }

        DateTime localStart = PublicTimeZoneResolver.ConvertUtcToLocal(startUtc, timeZoneInfo!);
        DateTime localEnd = PublicTimeZoneResolver.ConvertUtcToLocal(endUtc, timeZoneInfo!);
        DateOnly localDate = DateOnly.FromDateTime(localStart);

        if (DateOnly.FromDateTime(localEnd) != localDate)
        {
            return false;
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
                [staffMemberId],
                cancellationToken);

        if (availabilitySnapshot is null)
        {
            return false;
        }

        BranchWorkingHoursView? workingHours = availabilitySnapshot.WorkingHours
            .SingleOrDefault(entity => entity.DayOfWeek == localDate.DayOfWeek);

        if (workingHours is null || workingHours.IsClosed)
        {
            return false;
        }

        DateTime opensAtLocal = localDate.ToDateTime(workingHours.OpensAt, DateTimeKind.Unspecified);
        DateTime closesAtLocal = localDate.ToDateTime(workingHours.ClosesAt, DateTimeKind.Unspecified);

        if (localStart < opensAtLocal || localEnd > closesAtLocal)
        {
            return false;
        }

        if (availabilitySnapshot.StaffUnavailableTimes.Any(entity =>
            entity.StaffMemberId == staffMemberId
            && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc)))
        {
            return false;
        }

        IReadOnlyCollection<PublicResourceBlockView> resourceBlocks =
            await resourceAvailabilityQueryService.GetResourceBlocksAsync(
                [resourceId],
                startUtc,
                endUtc,
                cancellationToken);

        if (resourceBlocks.Any(entity =>
            entity.ResourceId == resourceId
            && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc)))
        {
            return false;
        }

        IReadOnlyCollection<ConfirmedAppointmentBusyTimeView> confirmedBusyTimes =
            await confirmedAppointmentQueryService.GetBusyTimesAsync(
                branch.Id,
                startUtc,
                endUtc,
                cancellationToken);

        return !confirmedBusyTimes.Any(entity =>
            (entity.StaffMemberId == staffMemberId || entity.ResourceId == resourceId)
            && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc));
    }

    private static bool IsRequestShapeValid(PublicAppointmentRequestCreateRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.BranchSlug)
            && request.ServiceVariantIds.Count > 0
            && request.ServiceVariantIds.All(entity => entity != Guid.Empty)
            && request.StaffMemberId != Guid.Empty
            && request.ResourceId != Guid.Empty
            && request.StartUtc != default;
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
}
