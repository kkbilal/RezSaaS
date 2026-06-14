namespace RezSaaS.Modules.Integrations.Application;

public sealed record CreateIntegrationApiClientCommand(
    Guid ActorUserAccountId,
    string DisplayName,
    IReadOnlyCollection<string> Scopes);
