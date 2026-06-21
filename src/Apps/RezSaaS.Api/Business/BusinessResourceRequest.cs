namespace RezSaaS.Api.Business;

public sealed record BusinessResourceCreateRequest(Guid ResourceTypeId, string DisplayName);
public sealed record BusinessResourceRenameRequest(string DisplayName);
