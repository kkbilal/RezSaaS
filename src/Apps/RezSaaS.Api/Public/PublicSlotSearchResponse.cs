namespace RezSaaS.Api.PublicApi;

public sealed record PublicSlotSearchResponse(
    string BusinessSlug,
    string BranchSlug,
    string BranchTimeZoneId,
    DateOnly Date,
    int DurationMinutes,
    IReadOnlyCollection<Guid> ServiceVariantIds,
    IReadOnlyCollection<PublicSlotResponse> Slots);
