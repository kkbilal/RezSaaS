namespace RezSaaS.Modules.Organization.Application;

public sealed record BusinessProfileSettingsView(
    Guid BusinessId,
    string Slug,
    string DisplayName,
    string CategoryKey,
    string Description,
    string PublicRules,
    string SeoTitle,
    string SeoDescription,
    string StaffDisplayPolicy,
    int CancellationCutoffHours);
