namespace RezSaaS.Api.Business;

public sealed record BusinessServiceCreateRequest(string Name, string CategoryKey);
public sealed record BusinessServiceUpdateRequest(string Name, string CategoryKey);
