namespace RezSaaS.Api.Customer;

public sealed record CustomerAppointmentHistoryItemResponse(
    string ItemType,
    Guid? AppointmentRequestId,
    Guid? AppointmentId,
    string BusinessSlug,
    string BusinessDisplayName,
    string BranchSlug,
    string BranchDisplayName,
    string BranchTimeZoneId,
    Guid StaffMemberId,
    string StaffMemberDisplayName,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Status,
    IReadOnlyCollection<CustomerAppointmentHistoryLineResponse> Lines);
