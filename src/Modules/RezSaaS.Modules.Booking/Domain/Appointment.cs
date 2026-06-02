namespace RezSaaS.Modules.Booking.Domain;

public sealed class Appointment
{
    private readonly List<AppointmentLine> lines = [];

    private Appointment()
    {
    }

    private Appointment(
        Guid id,
        Guid tenantId,
        Guid? appointmentRequestId,
        Guid customerUserAccountId,
        Guid branchId,
        Guid staffMemberId,
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(customerUserAccountId, nameof(customerUserAccountId));
        RequireNonEmpty(branchId, nameof(branchId));
        RequireNonEmpty(staffMemberId, nameof(staffMemberId));
        RequireNonEmpty(resourceId, nameof(resourceId));

        if (endUtc <= startUtc)
        {
            throw new ArgumentException("End must be later than start.", nameof(endUtc));
        }

        Id = id;
        TenantId = tenantId;
        AppointmentRequestId = appointmentRequestId;
        CustomerUserAccountId = customerUserAccountId;
        BranchId = branchId;
        StaffMemberId = staffMemberId;
        ResourceId = resourceId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid? AppointmentRequestId { get; private set; }

    public Guid BranchId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid CustomerUserAccountId { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public Guid Id { get; private set; }

    public IReadOnlyCollection<AppointmentLine> Lines => lines.AsReadOnly();

    public Guid ResourceId { get; private set; }

    public Guid StaffMemberId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Confirmed;

    public Guid TenantId { get; private set; }

    public static Appointment CreateConfirmed(
        Guid tenantId,
        Guid? appointmentRequestId,
        Guid customerUserAccountId,
        Guid branchId,
        Guid staffMemberId,
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        DateTimeOffset createdAtUtc)
    {
        return new Appointment(
            Guid.CreateVersion7(),
            tenantId,
            appointmentRequestId,
            customerUserAccountId,
            branchId,
            staffMemberId,
            resourceId,
            startUtc,
            endUtc,
            createdAtUtc);
    }

    public AppointmentLine AddLine(
        Guid serviceVariantId,
        string serviceNameSnapshot,
        int durationMinutes,
        decimal priceAmount,
        string currencyCode)
    {
        AppointmentLine line = AppointmentLine.Create(
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

    public void Cancel()
    {
        Status = AppointmentStatus.Cancelled;
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
