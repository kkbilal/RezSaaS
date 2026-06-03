namespace RezSaaS.Api.PublicApi;

public sealed record PublicSlotResourceResponse(
    Guid Id,
    Guid ResourceTypeId,
    string DisplayName);
