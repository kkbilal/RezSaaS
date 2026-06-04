namespace RezSaaS.Api.Configuration;

public sealed class PlatformNotificationWorkerOptions
{
    public const string SectionName = "Messaging:PlatformNotificationWorker";

    public int BatchSize { get; init; } = 50;

    public bool Enabled { get; init; } = true;

    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(1);

    public TimeSpan LockDuration { get; init; } = TimeSpan.FromMinutes(5);

    public int MaxAttempts { get; init; } = 10;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMinutes(5);
}
