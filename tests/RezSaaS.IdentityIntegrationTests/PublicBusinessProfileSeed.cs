namespace RezSaaS.IdentityIntegrationTests;

public sealed record PublicBusinessProfileSeed(
    string Slug,
    string BranchSlug,
    Guid ServiceVariantId,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset AvailableSlotStartUtc);
