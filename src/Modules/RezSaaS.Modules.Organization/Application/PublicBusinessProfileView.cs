namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessProfileView(
    string Slug,
    string DisplayName,
    string CategoryKey,
    string Description,
    IReadOnlyCollection<PublicBusinessBranchView> Branches);
