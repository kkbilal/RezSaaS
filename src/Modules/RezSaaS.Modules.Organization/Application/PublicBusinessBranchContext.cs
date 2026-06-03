namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessBranchContext(
    Guid Id,
    string Slug,
    string DisplayName,
    string TimeZoneId,
    string City,
    string District,
    string AddressLine,
    int? SlotIntervalMinutes,
    int? MaxPublicSlots,
    IReadOnlyCollection<PublicStaffMemberView> StaffMembers);
