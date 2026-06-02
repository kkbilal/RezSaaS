namespace RezSaaS.Modules.Identity.Configuration;

public sealed class IdentitySmtpEmailOptions
{
    public const string SectionName = "Identity:Smtp";

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = "RezSaaS";

    public string Host { get; init; } = string.Empty;

    public string? Password { get; init; }

    public int Port { get; init; }

    public string? UserName { get; init; }

    public bool UseSsl { get; init; } = true;

    public void Validate()
    {
        RequireText(Host, nameof(Host));
        RequireText(FromAddress, nameof(FromAddress));

        if (Port <= 0)
        {
            throw new InvalidOperationException(
                $"SMTP configuration value '{nameof(Port)}' must be greater than zero.");
        }
    }

    private static void RequireText(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"SMTP configuration value '{propertyName}' is required.");
        }
    }
}
