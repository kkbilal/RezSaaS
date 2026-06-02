namespace RezSaaS.Modules.Identity.Configuration;

public sealed class PlatformAdminBootstrapOptions
{
    public const string SectionName = "Identity:Bootstrap";

    public string PlatformAdminBootstrapTokenSha256 { get; init; } = string.Empty;

    public void ValidateForBootstrap()
    {
        if (string.IsNullOrWhiteSpace(PlatformAdminBootstrapTokenSha256))
        {
            throw new InvalidOperationException(
                "Platform admin bootstrap requires a SHA-256 bootstrap token hash.");
        }
    }
}
