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
        Guid? rebookedFromAppointmentId,
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
        RebookedFromAppointmentId = rebookedFromAppointmentId;
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

    public string? BusinessNote { get; private set; }

    public DateTimeOffset? BusinessNoteUpdatedAtUtc { get; private set; }

    public Guid? BusinessNoteUpdatedByUserAccountId { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public Guid? CancelledByUserAccountId { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public Guid? CompletedByUserAccountId { get; private set; }

    public string? CompletionNote { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid CustomerUserAccountId { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public Guid Id { get; private set; }

    public IReadOnlyCollection<AppointmentLine> Lines => lines.AsReadOnly();

    public Guid ResourceId { get; private set; }

    public Guid? RebookedFromAppointmentId { get; private set; }

    public DateTimeOffset? RebookedAtUtc { get; private set; }

    public Guid? RebookedByUserAccountId { get; private set; }

    public string? RebookReason { get; private set; }

    public Guid? RebookedToAppointmentId { get; private set; }

    public Guid StaffMemberId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Confirmed;

    public Guid TenantId { get; private set; }

    public DateTimeOffset? NoShowAtUtc { get; private set; }

    public Guid? NoShowByUserAccountId { get; private set; }

    public string? NoShowReason { get; private set; }

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
            rebookedFromAppointmentId: null,
            customerUserAccountId,
            branchId,
            staffMemberId,
            resourceId,
            startUtc,
            endUtc,
            createdAtUtc);
    }

    public static Appointment CreateRebookedConfirmed(
        Guid tenantId,
        Guid? appointmentRequestId,
        Guid rebookedFromAppointmentId,
        Guid customerUserAccountId,
        Guid branchId,
        Guid staffMemberId,
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(rebookedFromAppointmentId, nameof(rebookedFromAppointmentId));

        return new Appointment(
            Guid.CreateVersion7(),
            tenantId,
            appointmentRequestId,
            rebookedFromAppointmentId,
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

    public void Cancel(
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset cancelledAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        Status = AppointmentStatus.Cancelled;
        CancelledByUserAccountId = actorUserAccountId;
        CancelledAtUtc = cancelledAtUtc;
        CancellationReason = NormalizeRequiredText(reason, nameof(reason));
    }

    public void Complete(
        Guid actorUserAccountId,
        string? note,
        DateTimeOffset completedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        Status = AppointmentStatus.Completed;
        CompletedByUserAccountId = actorUserAccountId;
        CompletedAtUtc = completedAtUtc;
        CompletionNote = NormalizeOptionalText(note, maxLength: 500);
    }

    public void MarkNoShow(
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset markedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        Status = AppointmentStatus.NoShow;
        NoShowByUserAccountId = actorUserAccountId;
        NoShowAtUtc = markedAtUtc;
        NoShowReason = NormalizeRequiredText(reason, nameof(reason));
    }

    public void MarkRebooked(
        Guid newAppointmentId,
        Guid actorUserAccountId,
        string reason,
        DateTimeOffset rebookedAtUtc)
    {
        RequireNonEmpty(newAppointmentId, nameof(newAppointmentId));
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        Status = AppointmentStatus.Rebooked;
        RebookedToAppointmentId = newAppointmentId;
        RebookedByUserAccountId = actorUserAccountId;
        RebookedAtUtc = rebookedAtUtc;
        RebookReason = NormalizeRequiredText(reason, nameof(reason));
    }

    public void UpdateBusinessNote(
        Guid actorUserAccountId,
        string? note,
        DateTimeOffset updatedAtUtc)
    {
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        BusinessNote = NormalizeOptionalText(note, maxLength: 1_000);
        BusinessNoteUpdatedByUserAccountId = actorUserAccountId;
        BusinessNoteUpdatedAtUtc = updatedAtUtc;
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
