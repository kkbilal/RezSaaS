namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessSummaryView(
    Guid TenantId,
    string Slug,
    string DisplayName,
    string CategoryKey,
    string City,
    string District);
