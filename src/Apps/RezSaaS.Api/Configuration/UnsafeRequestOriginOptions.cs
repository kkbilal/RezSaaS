namespace RezSaaS.Api.Configuration;

public sealed class UnsafeRequestOriginOptions
{
    public const string SectionName = "Security:UnsafeRequestOrigins";

    public string[] AllowedOrigins { get; init; } = [];
}
