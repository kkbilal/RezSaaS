namespace RezSaaS.Api.PublicApi;

public sealed record PublicBusinessProfileResponse(
    string Slug,
    string DisplayName,
    string CategoryKey,
    string Description,
    IReadOnlyCollection<PublicBusinessBranchProfileResponse> Branches,
    IReadOnlyCollection<PublicServiceProfileResponse> Services);
