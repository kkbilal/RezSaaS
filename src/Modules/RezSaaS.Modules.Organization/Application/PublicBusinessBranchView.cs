namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessBranchView(
    string Slug,
    string DisplayName,
    string TimeZoneId,
    string City,
    string District,
    string AddressLine);
