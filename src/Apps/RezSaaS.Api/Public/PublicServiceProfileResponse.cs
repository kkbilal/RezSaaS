namespace RezSaaS.Api.PublicApi;

public sealed record PublicServiceProfileResponse(
    Guid Id,
    string Name,
    string CategoryKey,
    IReadOnlyCollection<PublicServiceVariantProfileResponse> Variants);
