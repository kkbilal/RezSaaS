namespace RezSaaS.Modules.Catalog.Application;

public sealed record PublicServiceMenuView(
    Guid Id,
    string Name,
    string CategoryKey,
    IReadOnlyCollection<PublicServiceVariantView> Variants);
