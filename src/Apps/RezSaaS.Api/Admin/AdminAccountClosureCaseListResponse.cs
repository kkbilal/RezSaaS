namespace RezSaaS.Api.Admin;

public sealed record AdminAccountClosureCaseListResponse(
    IReadOnlyCollection<AdminAccountClosureCaseResponse> ClosureCases);
