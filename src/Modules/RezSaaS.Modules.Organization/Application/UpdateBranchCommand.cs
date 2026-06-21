namespace RezSaaS.Modules.Organization.Application;

public sealed record UpdateBranchCommand(
    Guid ActorUserAccountId,
    Guid BranchId,
    string DisplayName,
    string City,
    string District,
    string AddressLine);

public sealed record UpdateBranchSlotSettingsCommand(
    Guid ActorUserAccountId,
    Guid BranchId,
    int? SlotIntervalMinutes,
    int? MaxPublicSlots);
