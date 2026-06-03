namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessCompositionContext(
    Guid TenantId,
    Guid BusinessId,
    string Slug,
    string DisplayName,
    string CategoryKey,
    string Description,
    string PublicRules,
    string SeoTitle,
    string SeoDescription,
    string StaffDisplayPolicy,
    decimal RatingAverage,
    int ReviewCount,
    IReadOnlyCollection<PublicBusinessGalleryImageView> GalleryImages,
    IReadOnlyCollection<PublicBusinessBranchContext> Branches);
