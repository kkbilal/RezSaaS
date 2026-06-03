using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;

namespace RezSaaS.Modules.Admin.Infrastructure.Auditing;

public sealed class AdminAuditLogRecorder : IAuditLogRecorder
{
    private readonly AdminDbContext dbContext;

    public AdminAuditLogRecorder(AdminDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task RecordAsync(
        AuditLogRecord record,
        CancellationToken cancellationToken = default)
    {
        AdminAuditLogEntry auditLogEntry = AdminAuditLogEntry.Create(
            record.ActorUserAccountId,
            record.Action,
            record.DetailsJson,
            record.OccurredAtUtc);

        dbContext.AdminAuditLogEntries.Add(auditLogEntry);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
