namespace RezSaaS.Api.Configuration;

public sealed class PlatformOperationsReconciliationOptions
{
    public const string SectionName = "Operations:Reconciliation";

    public TimeSpan CallbackPendingThreshold { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan ClosureExecutionStallThreshold { get; init; } = TimeSpan.FromMinutes(15);

    public bool Enabled { get; init; } = true;

    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromMinutes(1);

    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan NotificationOverdueThreshold { get; init; } = TimeSpan.FromMinutes(30);

    public int SampleSize { get; init; } = 10;

    public TimeSpan StaleProcessingThreshold { get; init; } = TimeSpan.FromMinutes(10);
}
