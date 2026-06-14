namespace RezSaaS.Modules.Integrations.Application;

internal sealed record IntegrationSecretMaterial(
    string Prefix,
    string Plaintext,
    string Sha256Hash);
