namespace RezSaaS.Modules.Organization.Application;

public sealed record PublicBusinessSearchQuery(
    string? SearchText,
    string? CategoryKey,
    string? City,
    string? District,
    int? Take);
