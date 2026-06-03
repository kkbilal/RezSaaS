using Microsoft.Extensions.Options;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Application;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Catalog.Application;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Resources.Application;

namespace RezSaaS.Api.PublicApi;

public sealed class PublicSlotSearchComposer
{
    private readonly AvailabilityQueryService availabilityQueryService;
    private readonly PublicBusinessDirectoryService businessDirectoryService;
    private readonly ConfirmedAppointmentQueryService confirmedAppointmentQueryService;
    private readonly PublicCatalogSchedulingService catalogSchedulingService;
    private readonly IOptions<PublicSlotSearchOptions> options;
    private readonly PublicResourceAvailabilityQueryService resourceAvailabilityQueryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicSlotSearchComposer(
        PublicBusinessDirectoryService businessDirectoryService,
        PublicCatalogSchedulingService catalogSchedulingService,
        AvailabilityQueryService availabilityQueryService,
        PublicResourceAvailabilityQueryService resourceAvailabilityQueryService,
        ConfirmedAppointmentQueryService confirmedAppointmentQueryService,
        ITenantContextAccessor tenantContextAccessor,
        IOptions<PublicSlotSearchOptions> options)
    {
        this.businessDirectoryService = businessDirectoryService;
        this.catalogSchedulingService = catalogSchedulingService;
        this.availabilityQueryService = availabilityQueryService;
        this.resourceAvailabilityQueryService = resourceAvailabilityQueryService;
        this.confirmedAppointmentQueryService = confirmedAppointmentQueryService;
        this.tenantContextAccessor = tenantContextAccessor;
        this.options = options;
    }

    public async Task<PublicSlotSearchResponse?> SearchAsync(
        string businessSlug,
        PublicSlotSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(
                businessSlug,
                cancellationToken);

        if (business is null)
        {
            return null;
        }

        PublicBusinessBranchContext? branch = business.Branches
            .SingleOrDefault(entity =>
                string.Equals(entity.Slug, request.BranchSlug, StringComparison.OrdinalIgnoreCase));

        if (branch is null)
        {
            return null;
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            Guid[] requestedVariantIds = request.ServiceVariantIds
                .Distinct()
                .ToArray();
            IReadOnlyCollection<PublicServiceVariantSchedulingView> variants =
                await catalogSchedulingService.GetVariantsAsync(
                    requestedVariantIds,
                    cancellationToken);

            if (variants.Count != requestedVariantIds.Length)
            {
                return null;
            }

            int durationMinutes = variants.Sum(entity => entity.DurationMinutes);
            PublicSlotSearchResponse emptyResponse = CreateResponse(
                business,
                branch,
                request,
                requestedVariantIds,
                durationMinutes,
                []);

            if (!TryFindTimeZone(branch.TimeZoneId, out TimeZoneInfo? timeZoneInfo))
            {
                return emptyResponse;
            }

            Guid[] requiredResourceTypeIds = variants
                .Select(entity => entity.RequiredResourceTypeId)
                .OfType<Guid>()
                .Distinct()
                .ToArray();

            if (requiredResourceTypeIds.Length > 1)
            {
                return emptyResponse;
            }

            Guid? requiredResourceTypeId = requiredResourceTypeIds.Length == 0
                ? null
                : requiredResourceTypeIds[0];
            PublicStaffMemberView[] staffCandidates = GetStaffCandidates(branch, request.StaffMemberId);

            if (staffCandidates.Length == 0)
            {
                return emptyResponse;
            }

            IReadOnlyCollection<PublicResourceCandidateView> resourceCandidates =
                await resourceAvailabilityQueryService.GetActiveResourcesAsync(
                    branch.Id,
                    requiredResourceTypeId,
                    cancellationToken);

            if (resourceCandidates.Count == 0)
            {
                return emptyResponse;
            }

            TimeZoneInfo resolvedTimeZoneInfo = timeZoneInfo!;
            DateTimeOffset dayStartUtc = ConvertLocalToUtc(
                request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
                resolvedTimeZoneInfo);
            DateTimeOffset dayEndUtc = ConvertLocalToUtc(
                request.Date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
                resolvedTimeZoneInfo);
            AvailabilitySnapshot? availabilitySnapshot =
                await availabilityQueryService.GetBranchSnapshotAsync(
                    branch.Id,
                    dayStartUtc,
                    dayEndUtc,
                    staffCandidates.Select(entity => entity.Id).ToArray(),
                    cancellationToken);

            if (availabilitySnapshot is null)
            {
                return emptyResponse;
            }

            BranchWorkingHoursView? workingHours = availabilitySnapshot.WorkingHours
                .SingleOrDefault(entity => entity.DayOfWeek == request.Date.DayOfWeek);

            if (workingHours is null || workingHours.IsClosed)
            {
                return emptyResponse;
            }

            Guid[] resourceIds = resourceCandidates
                .Select(entity => entity.Id)
                .ToArray();
            IReadOnlyCollection<PublicResourceBlockView> resourceBlocks =
                await resourceAvailabilityQueryService.GetResourceBlocksAsync(
                    resourceIds,
                    dayStartUtc,
                    dayEndUtc,
                    cancellationToken);
            IReadOnlyCollection<ConfirmedAppointmentBusyTimeView> confirmedBusyTimes =
                await confirmedAppointmentQueryService.GetBusyTimesAsync(
                    branch.Id,
                    dayStartUtc,
                    dayEndUtc,
                    cancellationToken);
            List<PublicSlotResponse> slots = GenerateSlots(
                request.Date,
                workingHours,
                durationMinutes,
                resolvedTimeZoneInfo,
                staffCandidates,
                resourceCandidates,
                availabilitySnapshot.StaffUnavailableTimes,
                resourceBlocks,
                confirmedBusyTimes);

            return CreateResponse(
                business,
                branch,
                request,
                requestedVariantIds,
                durationMinutes,
                slots);
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }

    private List<PublicSlotResponse> GenerateSlots(
        DateOnly date,
        BranchWorkingHoursView workingHours,
        int durationMinutes,
        TimeZoneInfo timeZoneInfo,
        IReadOnlyCollection<PublicStaffMemberView> staffCandidates,
        IReadOnlyCollection<PublicResourceCandidateView> resourceCandidates,
        IReadOnlyCollection<StaffUnavailableTimeView> staffUnavailableTimes,
        IReadOnlyCollection<PublicResourceBlockView> resourceBlocks,
        IReadOnlyCollection<ConfirmedAppointmentBusyTimeView> confirmedBusyTimes)
    {
        PublicSlotSearchOptions slotOptions = options.Value;
        List<PublicSlotResponse> slots = [];
        DateTime opensAtLocal = date.ToDateTime(workingHours.OpensAt, DateTimeKind.Unspecified);
        DateTime closesAtLocal = date.ToDateTime(workingHours.ClosesAt, DateTimeKind.Unspecified);
        DateTime latestStartLocal = closesAtLocal.AddMinutes(-durationMinutes);

        for (DateTime localStart = opensAtLocal;
            localStart <= latestStartLocal && slots.Count < slotOptions.MaxSlots;
            localStart = localStart.AddMinutes(slotOptions.SlotIntervalMinutes))
        {
            DateTime localEnd = localStart.AddMinutes(durationMinutes);
            DateTimeOffset startUtc = ConvertLocalToUtc(localStart, timeZoneInfo);
            DateTimeOffset endUtc = ConvertLocalToUtc(localEnd, timeZoneInfo);
            PublicSlotStaffResponse[] availableStaff = staffCandidates
                .Where(staff => IsStaffAvailable(
                    staff.Id,
                    startUtc,
                    endUtc,
                    staffUnavailableTimes,
                    confirmedBusyTimes))
                .Select(staff => new PublicSlotStaffResponse(
                    staff.Id,
                    staff.DisplayName))
                .ToArray();

            if (availableStaff.Length == 0)
            {
                continue;
            }

            PublicSlotResourceResponse[] availableResources = resourceCandidates
                .Where(resource => IsResourceAvailable(
                    resource.Id,
                    startUtc,
                    endUtc,
                    resourceBlocks,
                    confirmedBusyTimes))
                .Select(resource => new PublicSlotResourceResponse(
                    resource.Id,
                    resource.ResourceTypeId,
                    resource.DisplayName))
                .ToArray();

            if (availableResources.Length == 0)
            {
                continue;
            }

            slots.Add(new PublicSlotResponse(
                startUtc,
                endUtc,
                localStart,
                localEnd,
                availableStaff,
                availableResources));
        }

        return slots;
    }

    private static PublicSlotSearchResponse CreateResponse(
        PublicBusinessCompositionContext business,
        PublicBusinessBranchContext branch,
        PublicSlotSearchRequest request,
        IReadOnlyCollection<Guid> requestedVariantIds,
        int durationMinutes,
        IReadOnlyCollection<PublicSlotResponse> slots)
    {
        return new PublicSlotSearchResponse(
            business.Slug,
            branch.Slug,
            branch.TimeZoneId,
            request.Date,
            durationMinutes,
            requestedVariantIds,
            slots);
    }

    private static PublicStaffMemberView[] GetStaffCandidates(
        PublicBusinessBranchContext branch,
        Guid? staffMemberId)
    {
        IEnumerable<PublicStaffMemberView> staffMembers = branch.StaffMembers;

        if (staffMemberId is not null)
        {
            staffMembers = staffMembers.Where(entity => entity.Id == staffMemberId);
        }

        return staffMembers.ToArray();
    }

    private static bool IsStaffAvailable(
        Guid staffMemberId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        IReadOnlyCollection<StaffUnavailableTimeView> staffUnavailableTimes,
        IReadOnlyCollection<ConfirmedAppointmentBusyTimeView> confirmedBusyTimes)
    {
        return !staffUnavailableTimes.Any(entity =>
                entity.StaffMemberId == staffMemberId
                && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc))
            && !confirmedBusyTimes.Any(entity =>
                entity.StaffMemberId == staffMemberId
                && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc));
    }

    private static bool IsResourceAvailable(
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        IReadOnlyCollection<PublicResourceBlockView> resourceBlocks,
        IReadOnlyCollection<ConfirmedAppointmentBusyTimeView> confirmedBusyTimes)
    {
        return !resourceBlocks.Any(entity =>
                entity.ResourceId == resourceId
                && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc))
            && !confirmedBusyTimes.Any(entity =>
                entity.ResourceId == resourceId
                && Overlaps(startUtc, endUtc, entity.StartUtc, entity.EndUtc));
    }

    private static bool Overlaps(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        DateTimeOffset busyStartUtc,
        DateTimeOffset busyEndUtc)
    {
        return startUtc < busyEndUtc && endUtc > busyStartUtc;
    }

    private static DateTimeOffset ConvertLocalToUtc(
        DateTime localTime,
        TimeZoneInfo timeZoneInfo)
    {
        DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localTime, timeZoneInfo);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }

    private static bool TryFindTimeZone(
        string timeZoneId,
        out TimeZoneInfo? timeZoneInfo)
    {
        if (TryFindTimeZoneById(timeZoneId, out timeZoneInfo))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out string? windowsTimeZoneId)
            && TryFindTimeZoneById(windowsTimeZoneId, out timeZoneInfo))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId, out string? ianaTimeZoneId)
            && TryFindTimeZoneById(ianaTimeZoneId, out timeZoneInfo))
        {
            return true;
        }

        timeZoneInfo = null;
        return false;
    }

    private static bool TryFindTimeZoneById(
        string timeZoneId,
        out TimeZoneInfo? timeZoneInfo)
    {
        try
        {
            timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZoneInfo = null;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            timeZoneInfo = null;
            return false;
        }
    }
}
