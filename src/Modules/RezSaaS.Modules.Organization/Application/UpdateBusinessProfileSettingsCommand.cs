namespace RezSaaS.Modules.Organization.Application;

public sealed record UpdateBusinessProfileSettingsCommand(
    Guid ActorUserAccountId,
    string DisplayName,
    string Description,
    string PublicRules,
    string SeoTitle,
    string SeoDescription,
    string StaffDisplayPolicy);
