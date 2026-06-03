namespace RezSaaS.Api.PublicApi;

public sealed record PublicBusinessProfileResponse(
    string Slug,
    string DisplayName,
    string CategoryKey,
    string Description,
    PublicBusinessProfileMetadataResponse Metadata,
    IReadOnlyCollection<PublicBusinessBranchProfileResponse> Branches,
    IReadOnlyCollection<PublicServiceProfileResponse> Services);
