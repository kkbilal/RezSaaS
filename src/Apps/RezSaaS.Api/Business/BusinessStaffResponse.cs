namespace RezSaaS.Api.Business;

public sealed record BusinessStaffResponse(
    Guid Id,
    Guid BranchId,
    string DisplayName,
    Guid? UserAccountId,
    string Status,
    DateTimeOffset CreatedAtUtc);
