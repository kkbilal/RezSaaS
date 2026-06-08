namespace RezSaaS.IdentityIntegrationTests;

public sealed record PublicBusinessProfileSeed(
    Guid TenantId,
    Guid BranchId,
    string Slug,
    string BranchSlug,
    Guid ServiceVariantId,
    Guid RequiredSkillId,
    Guid StaffMemberId,
    Guid? UnqualifiedStaffMemberId,
    Guid ResourceId,
    Guid ConfirmedAppointmentId,
    DateTimeOffset ConfirmedAppointmentStartUtc,
    DateTimeOffset ConfirmedAppointmentEndUtc,
    DateTimeOffset AvailableSlotStartUtc);
