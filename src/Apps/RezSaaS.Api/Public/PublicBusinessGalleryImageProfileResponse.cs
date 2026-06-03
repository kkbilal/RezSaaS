namespace RezSaaS.Api.PublicApi;

public sealed record PublicBusinessGalleryImageProfileResponse(
    string ImageUrl,
    string AltText,
    int SortOrder);
