namespace RezSaaS.Api.PublicApi;

public sealed record PublicSlotSearchRequest(
    string BranchSlug,
    DateOnly Date,
    IReadOnlyCollection<Guid> ServiceVariantIds,
    Guid? StaffMemberId);
