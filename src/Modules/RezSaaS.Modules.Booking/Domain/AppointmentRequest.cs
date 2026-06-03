namespace RezSaaS.Modules.Booking.Domain;

public sealed class AppointmentRequest
{
    private readonly List<AppointmentRequestLine> lines = [];

    private AppointmentRequest()
    {
    }

    private AppointmentRequest(
        Guid id,
        Guid tenantId,
        Guid customerUserAccountId,
        Guid branchId,
        Guid staffMemberId,
        Guid resourceId,
        DateTimeOffset requestedStartUtc,
        DateTimeOffset requestedEndUtc,
        DateTimeOffset createdAtUtc,
        TimeSpan responseBuffer)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(customerUserAccountId, nameof(customerUserAccountId));
        RequireNonEmpty(branchId, nameof(branchId));
        RequireNonEmpty(staffMemberId, nameof(staffMemberId));
        RequireNonEmpty(resourceId, nameof(resourceId));

        if (requestedEndUtc <= requestedStartUtc)
        {
            throw new ArgumentException("End must be later than start.", nameof(requestedEndUtc));
        }

        Id = id;
        TenantId = tenantId;
        CustomerUserAccountId = customerUserAccountId;
        BranchId = branchId;
        StaffMemberId = staffMemberId;
        ResourceId = resourceId;
        RequestedStartUtc = requestedStartUtc;
        RequestedEndUtc = requestedEndUtc;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = CalculateExpiry(createdAtUtc, requestedStartUtc, responseBuffer);
    }

    public Guid BranchId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid CustomerUserAccountId { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public IReadOnlyCollection<AppointmentRequestLine> Lines => lines.AsReadOnly();

    public Guid ResourceId { get; private set; }

    public DateTimeOffset RequestedEndUtc { get; private set; }

    public DateTimeOffset RequestedStartUtc { get; private set; }

    public Guid StaffMemberId { get; private set; }

    public AppointmentRequestStatus Status { get; private set; } = AppointmentRequestStatus.PendingApproval;

    public Guid TenantId { get; private set; }

    public static AppointmentRequest Create(
        Guid tenantId,
        Guid customerUserAccountId,
        Guid branchId,
        Guid staffMemberId,
        Guid resourceId,
        DateTimeOffset requestedStartUtc,
        DateTimeOffset requestedEndUtc,
        DateTimeOffset createdAtUtc,
        TimeSpan responseBuffer)
    {
        return new AppointmentRequest(
            Guid.CreateVersion7(),
            tenantId,
            customerUserAccountId,
            branchId,
            staffMemberId,
            resourceId,
            requestedStartUtc,
            requestedEndUtc,
            createdAtUtc,
            responseBuffer);
    }

    public AppointmentRequestLine AddLine(
        Guid serviceVariantId,
        string serviceNameSnapshot,
        int durationMinutes,
        decimal priceAmount,
        string currencyCode)
    {
        AppointmentRequestLine line = AppointmentRequestLine.Create(
            TenantId,
            Id,
            serviceVariantId,
            serviceNameSnapshot,
            durationMinutes,
            priceAmount,
            currencyCode);

        lines.Add(line);

        return line;
    }

    public void Approve()
    {
        Status = AppointmentRequestStatus.Approved;
    }

    public void Decline()
    {
        Status = AppointmentRequestStatus.Declined;
    }

    public void CancelByCustomer()
    {
        Status = AppointmentRequestStatus.CancelledByCustomer;
    }

    public void Expire()
    {
        Status = AppointmentRequestStatus.Expired;
    }

    public void Supersede()
    {
        Status = AppointmentRequestStatus.Superseded;
    }

    private static DateTimeOffset CalculateExpiry(
        DateTimeOffset createdAtUtc,
        DateTimeOffset requestedStartUtc,
        TimeSpan responseBuffer)
    {
        DateTimeOffset maxTtlExpiry = createdAtUtc.AddHours(24);
        DateTimeOffset responseBufferExpiry = requestedStartUtc.Subtract(responseBuffer);

        return responseBufferExpiry < maxTtlExpiry ? responseBufferExpiry : maxTtlExpiry;
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
