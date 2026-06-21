namespace RezSaaS.Api.Business;

public sealed record BusinessStaffCreateRequest(
    string DisplayName,
    Guid? UserAccountId);

public sealed record BusinessStaffUpdateRequest(string DisplayName);
