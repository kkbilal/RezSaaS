using RezSaaS.BuildingBlocks.Abuse;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Infrastructure.Abuse;

public sealed class AdminAbuseEventRecorder : IAbuseEventRecorder
{
    private readonly AdminDbContext dbContext;

    public AdminAbuseEventRecorder(AdminDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task RecordAsync(
        AbuseEventRecord record,
        CancellationToken cancellationToken = default)
    {
        AbuseEvent abuseEvent = AbuseEvent.Create(
            record.TenantId,
            record.UserAccountId,
            record.EventType,
            MapSeverity(record.Severity),
            record.DetailsJson,
            record.OccurredAtUtc);

        dbContext.AbuseEvents.Add(abuseEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AbuseEventSeverity MapSeverity(AbuseEventSeverityLevel severity)
    {
        return severity switch
        {
            AbuseEventSeverityLevel.Low => AbuseEventSeverity.Low,
            AbuseEventSeverityLevel.Medium => AbuseEventSeverity.Medium,
            AbuseEventSeverityLevel.High => AbuseEventSeverity.High,
            AbuseEventSeverityLevel.Critical => AbuseEventSeverity.Critical,
            _ => AbuseEventSeverity.Medium,
        };
    }
}
