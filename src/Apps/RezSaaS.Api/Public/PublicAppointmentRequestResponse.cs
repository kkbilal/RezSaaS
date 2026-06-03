namespace RezSaaS.Api.PublicApi;

public sealed record PublicAppointmentRequestResponse(
    Guid Id,
    string BusinessSlug,
    string BranchSlug,
    string BranchDisplayName,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset RequestedStartUtc,
    DateTimeOffset RequestedEndUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    IReadOnlyCollection<PublicAppointmentRequestLineResponse> Lines);
