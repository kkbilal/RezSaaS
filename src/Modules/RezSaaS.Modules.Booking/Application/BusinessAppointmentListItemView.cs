namespace RezSaaS.Modules.Booking.Application;

public sealed record BusinessAppointmentListItemView(
    Guid Id,
    Guid? AppointmentRequestId,
    Guid CustomerUserAccountId,
    Guid BranchId,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Status,
    string? BusinessNote,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason,
    DateTimeOffset? CompletedAtUtc,
    string? CompletionNote,
    DateTimeOffset? NoShowAtUtc,
    string? NoShowReason,
    Guid? RebookedFromAppointmentId,
    Guid? RebookedToAppointmentId,
    DateTimeOffset? RebookedAtUtc,
    string? RebookReason,
    IReadOnlyCollection<BusinessAppointmentLineView> Lines);
