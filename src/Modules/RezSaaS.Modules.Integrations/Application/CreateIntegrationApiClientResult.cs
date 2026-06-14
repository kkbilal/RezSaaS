namespace RezSaaS.Modules.Integrations.Application;

public sealed record CreateIntegrationApiClientResult(
    Guid ApiClientId,
    string KeyPrefix,
    string OneTimePlaintextApiKey,
    DateTimeOffset CreatedAtUtc);
