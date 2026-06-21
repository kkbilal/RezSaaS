namespace RezSaaS.Api.Business;

public sealed record BusinessBranchCreateRequest(
    string Slug,
    string DisplayName,
    string TimeZoneId,
    string? City,
    string? District,
    string? AddressLine);

public sealed record BusinessBranchUpdateRequest(
    string? DisplayName,
    string? City,
    string? District,
    string? AddressLine);

public sealed record BusinessBranchSlotSettingsRequest(
    int? SlotIntervalMinutes,
    int? MaxPublicSlots);
