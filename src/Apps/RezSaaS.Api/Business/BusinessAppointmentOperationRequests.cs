namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentCancelRequest(string Reason);

public sealed record BusinessAppointmentCompleteRequest(string? Note);

public sealed record BusinessAppointmentNoShowRequest(string Reason);

public sealed record BusinessAppointmentNoteRequest(string? Note);

public sealed record BusinessAppointmentRebookRequest(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    Guid? StaffMemberId,
    Guid? ResourceId,
    string Reason);
