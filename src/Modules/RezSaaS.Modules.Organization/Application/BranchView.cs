namespace RezSaaS.Modules.Organization.Application;

public sealed record BranchView(
    Guid Id,
    string Slug,
    string DisplayName,
    string TimeZoneId,
    string City,
    string District,
    string AddressLine,
    int? SlotIntervalMinutes,
    int? MaxPublicSlots,
    DateTimeOffset CreatedAtUtc);
