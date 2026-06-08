using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Resources.Domain;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;

namespace RezSaaS.Modules.Resources.Application;

public sealed class ResourceOperationalBlockService
{
    private const string Conflict = "RESOURCE_BLOCK_CONFLICT";
    private const string InvalidTimeRange = "RESOURCE_BLOCK_INVALID_TIME_RANGE";
    private const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    private const string NotFound = "RESOURCE_NOT_FOUND";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly ResourcesDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public ResourceOperationalBlockService(
        ResourcesDbContext dbContext,
        IAuditLogRecorder auditLogRecorder,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.auditLogRecorder = auditLogRecorder;
        this.tenantContextAccessor = tenantContextAccessor;
        this.timeProvider = timeProvider;
    }

    public async Task<Guid?> GetResourceBranchIdAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null || resourceId == Guid.Empty)
        {
            return null;
        }

        return await dbContext.Resources
            .AsNoTracking()
            .Where(entity => entity.Id == resourceId)
            .Select(entity => (Guid?)entity.BranchId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ResourceBlockCommandResult> CreateBlockAsync(
        Guid resourceId,
        Guid actorUserAccountId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return ResourceBlockCommandResult.Failure(MissingTenantContext);
        }

        if (endUtc <= startUtc)
        {
            return ResourceBlockCommandResult.Failure(InvalidTimeRange);
        }

        Resource? resource = await dbContext.Resources
            .SingleOrDefaultAsync(entity => entity.Id == resourceId, cancellationToken);

        if (resource is null)
        {
            return ResourceBlockCommandResult.Failure(NotFound);
        }

        ResourceBlock? existingBlock = await dbContext.ResourceBlocks
            .AsNoTracking()
            .Where(entity => entity.ResourceId == resourceId
                && entity.StartUtc == startUtc
                && entity.EndUtc == endUtc)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingBlock is not null)
        {
            return ResourceBlockCommandResult.Success(
                ToView(existingBlock, resource.BranchId));
        }

        bool hasOverlap = await dbContext.ResourceBlocks
            .AsNoTracking()
            .AnyAsync(
                entity => entity.ResourceId == resourceId
                    && entity.StartUtc < endUtc
                    && entity.EndUtc > startUtc,
                cancellationToken);

        if (hasOverlap)
        {
            return ResourceBlockCommandResult.Failure(Conflict);
        }

        ResourceBlock block = ResourceBlock.Create(
            tenantId,
            resourceId,
            startUtc,
            endUtc,
            reason);

        dbContext.ResourceBlocks.Add(block);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(
            new AuditLogRecord(
                tenantId,
                actorUserAccountId,
                "resources.resource.blocked",
                $$"""{"tenantId":"{{tenantId}}","resourceId":"{{resourceId}}","resourceBlockId":"{{block.Id}}"}""",
                now),
            cancellationToken);

        return ResourceBlockCommandResult.Success(ToView(block, resource.BranchId));
    }

    private static ResourceBlockView ToView(ResourceBlock block, Guid branchId)
    {
        return new ResourceBlockView(
            block.Id,
            block.ResourceId,
            branchId,
            block.StartUtc,
            block.EndUtc,
            block.Reason);
    }
}
