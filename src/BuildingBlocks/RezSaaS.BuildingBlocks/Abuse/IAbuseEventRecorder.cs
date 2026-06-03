namespace RezSaaS.BuildingBlocks.Abuse;

public interface IAbuseEventRecorder
{
    Task RecordAsync(
        AbuseEventRecord record,
        CancellationToken cancellationToken = default);
}
