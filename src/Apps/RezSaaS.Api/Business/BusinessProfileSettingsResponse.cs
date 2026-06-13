namespace RezSaaS.Api.Business;

public sealed record BusinessProfileSettingsResponse(
    Guid BusinessId,
    string Slug,
    string DisplayName,
    string CategoryKey,
    string Description,
    string PublicRules,
    string SeoTitle,
    string SeoDescription,
    string StaffDisplayPolicy);
