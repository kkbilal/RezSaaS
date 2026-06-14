using System.Security.Cryptography;
using System.Text;

namespace RezSaaS.Modules.Integrations.Application;

internal static class IntegrationSecretFactory
{
    public static IntegrationSecretMaterial CreateApiKey()
    {
        string publicIdentifier = ToBase64Url(RandomNumberGenerator.GetBytes(10));
        string secret = ToBase64Url(RandomNumberGenerator.GetBytes(32));
        string prefix = $"rzs_live_{publicIdentifier}";
        string plaintext = $"{prefix}_{secret}";

        return new IntegrationSecretMaterial(
            prefix,
            plaintext,
            CreateSha256Hex(plaintext));
    }

    public static IntegrationSecretMaterial CreateWebhookSigningSecret()
    {
        string plaintext = $"whsec_{ToBase64Url(RandomNumberGenerator.GetBytes(32))}";

        return new IntegrationSecretMaterial(
            Prefix: string.Empty,
            plaintext,
            CreateSha256Hex(plaintext));
    }

    public static string CreateSha256Hex(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal);
    }
}
