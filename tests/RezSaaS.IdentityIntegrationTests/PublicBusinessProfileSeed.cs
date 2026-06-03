namespace RezSaaS.IdentityIntegrationTests;

public sealed record PublicBusinessProfileSeed(
    Guid TenantId,
    Guid BranchId,
    string Slug,
    string BranchSlug,
    Guid ServiceVariantId,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset AvailableSlotStartUtc);
