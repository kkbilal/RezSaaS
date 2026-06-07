namespace RezSaaS.Modules.Organization.Application;

public sealed record BranchLabelView(
    Guid Id,
    string Slug,
    string DisplayName,
    string TimeZoneId);
