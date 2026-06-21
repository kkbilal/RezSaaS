namespace RezSaaS.Modules.Organization.Application;

public sealed record StaffView(
    Guid Id,
    Guid BranchId,
    string DisplayName,
    Guid? UserAccountId,
    string Status,
    DateTimeOffset CreatedAtUtc);
