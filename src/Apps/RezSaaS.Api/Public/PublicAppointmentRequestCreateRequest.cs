namespace RezSaaS.Api.PublicApi;

public sealed record PublicAppointmentRequestCreateRequest(
    string BranchSlug,
    IReadOnlyCollection<Guid> ServiceVariantIds,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset StartUtc);
