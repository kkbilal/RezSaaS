namespace RezSaaS.Api.PublicApi;

public sealed record PublicBusinessProfileMetadataResponse(
    string PublicRules,
    string SeoTitle,
    string SeoDescription,
    string StaffDisplayPolicy,
    decimal RatingAverage,
    int ReviewCount,
    IReadOnlyCollection<PublicBusinessGalleryImageProfileResponse> GalleryImages);
