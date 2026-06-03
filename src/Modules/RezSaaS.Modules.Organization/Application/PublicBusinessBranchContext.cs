namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessBranchContext(
    Guid Id,
    string Slug,
    string DisplayName,
    string TimeZoneId,
    string City,
    string District,
    string AddressLine,
    IReadOnlyCollection<PublicStaffMemberView> StaffMembers);
