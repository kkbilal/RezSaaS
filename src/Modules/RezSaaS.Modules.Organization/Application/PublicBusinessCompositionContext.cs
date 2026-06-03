namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessCompositionContext(
    Guid TenantId,
    Guid BusinessId,
    string Slug,
    string DisplayName,
    string CategoryKey,
    string Description,
    IReadOnlyCollection<PublicBusinessBranchContext> Branches);
