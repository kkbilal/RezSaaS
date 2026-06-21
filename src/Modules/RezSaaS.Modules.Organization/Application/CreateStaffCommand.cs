namespace RezSaaS.Modules.Organization.Application;

public sealed record CreateStaffCommand(
    Guid ActorUserAccountId,
    Guid BranchId,
    string DisplayName,
    Guid? UserAccountId);

public sealed record UpdateStaffCommand(
    Guid ActorUserAccountId,
    Guid BranchId,
    Guid StaffId,
    string DisplayName);
