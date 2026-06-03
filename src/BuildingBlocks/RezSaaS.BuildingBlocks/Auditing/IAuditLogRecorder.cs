namespace RezSaaS.BuildingBlocks.Auditing;

public interface IAuditLogRecorder
{
    Task RecordAsync(
        AuditLogRecord record,
        CancellationToken cancellationToken = default);
}
