namespace RezSaaS.Api.Session;

public sealed class SessionRateLimitOptions
{
    public const string SectionName = "SessionRateLimit";

    public int PermitLimit { get; init; } = 120;

    public int WindowMinutes { get; init; } = 1;
}
