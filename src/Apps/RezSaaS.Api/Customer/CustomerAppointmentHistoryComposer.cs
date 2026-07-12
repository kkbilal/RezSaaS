using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Customer;

public sealed class CustomerAppointmentHistoryComposer
{
    private const string InvalidStatus = "CUSTOMER_APPOINTMENT_HISTORY_INVALID_STATUS";
    private const string Unauthorized = "CUSTOMER_APPOINTMENT_HISTORY_UNAUTHORIZED";

    private readonly BusinessEntityLabelQueryService businessEntityLabelQueryService;
    private readonly ConfirmedAppointmentQueryService confirmedAppointmentQueryService;
    private readonly CustomerAppointmentRequestQueryService customerAppointmentRequestQueryService;
    private readonly TenantLifecycleQueryService tenantLifecycleQueryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public CustomerAppointmentHistoryComposer(
        TenantLifecycleQueryService tenantLifecycleQueryService,
        BusinessEntityLabelQueryService businessEntityLabelQueryService,
        CustomerAppointmentRequestQueryService customerAppointmentRequestQueryService,
        ConfirmedAppointmentQueryService confirmedAppointmentQueryService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.tenantLifecycleQueryService = tenantLifecycleQueryService;
        this.businessEntityLabelQueryService = businessEntityLabelQueryService;
        this.customerAppointmentRequestQueryService = customerAppointmentRequestQueryService;
        this.confirmedAppointmentQueryService = confirmedAppointmentQueryService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<CustomerAppointmentHistoryResult> GetAsync(
        ClaimsPrincipal user,
        string? status,
        int? take,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid customerUserAccountId))
        {
            return CustomerAppointmentHistoryResult.Failure(
                CustomerAppointmentHistoryOutcome.Unauthorized,
                Unauthorized);
        }

        // BUG FIX: burada AppointmentRequestStatusFilter kullaniliyordu -- yani yalnizca TALEP
        // statuleri gecerli sayiliyordu. Bu uc TEK listede IKI aggregate donuyor ve iki status
        // enum'unun KESISIMI BOS:
        //   Talep    : PendingApproval, Approved, Declined, Expired, Superseded, CancelledByCustomer
        //   Randevu  : Confirmed, Cancelled, Completed, NoShow, Rebooked
        // Sonuc: ?status=Confirmed -> 400; ?status=PendingApproval -> 200 ama randevular BOS.
        // Yani HANGI DEGERI VERIRSENIZ VERIN randevular gecmiste HIC GORUNMUYORDU.
        if (!CustomerHistoryStatusFilter.IsValidOrEmpty(status))
        {
            return CustomerAppointmentHistoryResult.Failure(
                CustomerAppointmentHistoryOutcome.BadRequest,
                InvalidStatus);
        }

        int boundedTake = Math.Clamp(take ?? 50, 1, 100);
        List<CustomerAppointmentHistoryItemResponse> items = [];
        IReadOnlyCollection<Guid> tenantIds =
            await tenantLifecycleQueryService.GetTenantIdsAsync(
                cancellationToken: cancellationToken);
        Guid? previousTenantId = tenantContextAccessor.TenantId;

        try
        {
            foreach (Guid tenantId in tenantIds)
            {
                tenantContextAccessor.TenantId = tenantId;
                BusinessLabelView? business =
                    await businessEntityLabelQueryService.GetBusinessLabelAsync(cancellationToken);

                if (business is null)
                {
                    continue;
                }

                IReadOnlyDictionary<Guid, BranchLabelView> branches =
                    await businessEntityLabelQueryService.GetAllBranchLabelsAsync(cancellationToken);
                Guid[] branchIds = branches.Keys.ToArray();

                if (branchIds.Length == 0)
                {
                    continue;
                }

                IReadOnlyCollection<CustomerConfirmedAppointmentView> appointments =
                    await confirmedAppointmentQueryService.GetOwnAsync(
                        customerUserAccountId,
                        branchIds,
                        // BUG FIX: status BURAYA HIC GECIRILMIYORDU. Yalnizca taleplere
                        // uygulaniyordu, randevulara degil -> ?status=X gonderilse bile TUM
                        // randevular donuyordu ve /hesabim sekmeleri yanlis veri gosteriyordu.
                        status,
                        boundedTake,
                        cancellationToken);
                HashSet<Guid> appointmentRequestIdsWithAppointment = appointments
                    .Select(entity => entity.AppointmentRequestId)
                    .OfType<Guid>()
                    .ToHashSet();
                IReadOnlyCollection<CustomerAppointmentRequestView> requests =
                    await customerAppointmentRequestQueryService.GetOwnAsync(
                        customerUserAccountId,
                        branchIds,
                        status,
                        boundedTake,
                        cancellationToken);
                IReadOnlyDictionary<Guid, StaffMemberLabelView> staffMembers =
                    await businessEntityLabelQueryService.GetStaffMemberLabelsAsync(
                        requests.Select(entity => entity.StaffMemberId)
                            .Concat(appointments.Select(entity => entity.StaffMemberId))
                            .ToArray(),
                        cancellationToken);

                items.AddRange(appointments.Select(entity =>
                    ToAppointmentResponse(business, branches, staffMembers, entity)));
                items.AddRange(requests
                    .Where(entity => !appointmentRequestIdsWithAppointment.Contains(entity.Id))
                    .Select(entity => ToRequestResponse(business, branches, staffMembers, entity)));
            }
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }

        return CustomerAppointmentHistoryResult.Success(
            items
                .OrderByDescending(entity => entity.StartUtc)
                .Take(boundedTake)
                .ToArray());
    }

    private static CustomerAppointmentHistoryItemResponse ToAppointmentResponse(
        BusinessLabelView business,
        IReadOnlyDictionary<Guid, BranchLabelView> branches,
        IReadOnlyDictionary<Guid, StaffMemberLabelView> staffMembers,
        CustomerConfirmedAppointmentView appointment)
    {
        branches.TryGetValue(appointment.BranchId, out BranchLabelView? branch);
        staffMembers.TryGetValue(appointment.StaffMemberId, out StaffMemberLabelView? staff);

        return new CustomerAppointmentHistoryItemResponse(
            "Appointment",
            appointment.AppointmentRequestId,
            appointment.Id,
            business.Slug,
            business.DisplayName,
            branch?.Slug ?? string.Empty,
            branch?.DisplayName ?? string.Empty,
            branch?.TimeZoneId ?? string.Empty,
            appointment.StaffMemberId,
            staff?.DisplayName ?? string.Empty,
            appointment.StartUtc,
            appointment.EndUtc,
            ExpiresAtUtc: null,
            appointment.Status,
            appointment.Lines
                .Select(line => new CustomerAppointmentHistoryLineResponse(
                    line.ServiceVariantId,
                    line.ServiceNameSnapshot,
                    line.DurationMinutes,
                    line.PriceAmount,
                    line.CurrencyCode))
                .ToArray());
    }

    private static CustomerAppointmentHistoryItemResponse ToRequestResponse(
        BusinessLabelView business,
        IReadOnlyDictionary<Guid, BranchLabelView> branches,
        IReadOnlyDictionary<Guid, StaffMemberLabelView> staffMembers,
        CustomerAppointmentRequestView request)
    {
        branches.TryGetValue(request.BranchId, out BranchLabelView? branch);
        staffMembers.TryGetValue(request.StaffMemberId, out StaffMemberLabelView? staff);

        return new CustomerAppointmentHistoryItemResponse(
            "AppointmentRequest",
            request.Id,
            AppointmentId: null,
            business.Slug,
            business.DisplayName,
            branch?.Slug ?? string.Empty,
            branch?.DisplayName ?? string.Empty,
            branch?.TimeZoneId ?? string.Empty,
            request.StaffMemberId,
            staff?.DisplayName ?? string.Empty,
            request.RequestedStartUtc,
            request.RequestedEndUtc,
            request.ExpiresAtUtc,
            request.Status,
            request.Lines
                .Select(line => new CustomerAppointmentHistoryLineResponse(
                    line.ServiceVariantId,
                    line.ServiceNameSnapshot,
                    line.DurationMinutes,
                    line.PriceAmount,
                    line.CurrencyCode))
                .ToArray());
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
