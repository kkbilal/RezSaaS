namespace RezSaaS.Api.Business;

public sealed class AppointmentRequestExpiryWorkerOptions
{
    public const string SectionName = "Booking:ExpiryWorker";

    public bool Enabled { get; init; } = true;

    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(5);

    public int TenantBatchSize { get; init; } = 500;
}
