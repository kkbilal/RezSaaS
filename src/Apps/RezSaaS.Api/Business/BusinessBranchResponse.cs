namespace RezSaaS.Api.Business;

public sealed record BusinessBranchResponse(
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
