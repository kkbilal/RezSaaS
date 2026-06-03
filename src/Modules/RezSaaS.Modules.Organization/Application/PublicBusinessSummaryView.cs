namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessSummaryView(
    string Slug,
    string DisplayName,
    string CategoryKey,
    string City,
    string District);
