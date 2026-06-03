namespace RezSaaS.Api.PublicApi;

public sealed record PublicBusinessBranchProfileResponse(
    string Slug,
    string DisplayName,
    string TimeZoneId,
    string City,
    string District,
    string AddressLine,
    IReadOnlyCollection<PublicStaffMemberProfileResponse> StaffMembers,
    IReadOnlyCollection<PublicBranchWorkingHoursProfileResponse> WorkingHours);
