namespace RezSaaS.Modules.Organization.Application;

public sealed record CreateBranchCommand(
    Guid ActorUserAccountId,
    string Slug,
    string DisplayName,
    string TimeZoneId,
    string City,
    string District,
    string AddressLine);
