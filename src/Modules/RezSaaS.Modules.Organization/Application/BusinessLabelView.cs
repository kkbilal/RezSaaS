namespace RezSaaS.Modules.Organization.Application;

public sealed record BusinessLabelView(
    Guid TenantId,
    Guid BusinessId,
    string Slug,
    string DisplayName);
