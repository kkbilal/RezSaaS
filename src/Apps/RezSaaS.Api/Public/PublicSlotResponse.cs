namespace RezSaaS.Api.PublicApi;

public sealed record PublicSlotResponse(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    DateTime LocalStart,
    DateTime LocalEnd,
    IReadOnlyCollection<PublicSlotStaffResponse> StaffCandidates,
    IReadOnlyCollection<PublicSlotResourceResponse> ResourceCandidates);
