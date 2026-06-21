namespace RezSaaS.Api.Business;

public sealed record BusinessResourceResponse(Guid Id, Guid ResourceTypeId, string DisplayName, string Status);
