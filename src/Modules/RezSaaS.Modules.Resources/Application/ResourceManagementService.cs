using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Resources.Domain;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;

namespace RezSaaS.Modules.Resources.Application;

public sealed class ResourceManagementService
{
    public const string InvalidRequest = "RESOURCE_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string ResourceTypeNotFound = "RESOURCE_TYPE_NOT_FOUND";
    public const string ResourceTypeInactive = "RESOURCE_TYPE_INACTIVE";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly ResourcesDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public ResourceManagementService(
        ResourcesDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        IAuditLogRecorder auditLogRecorder,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
        this.auditLogRecorder = auditLogRecorder;
        this.timeProvider = timeProvider;
    }

    public async Task<ResourceManagementResult> ListByBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return ResourceManagementResult.Failure(MissingTenantContext);

        List<ResourceView> resources = await dbContext.Resources
            .AsNoTracking()
            .Where(entity => entity.BranchId == branchId)
            .OrderBy(entity => entity.DisplayName)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return ResourceManagementResult.SuccessList(resources);
    }

    public async Task<ResourceManagementResult> CreateAsync(Guid actorUserAccountId, Guid branchId, Guid resourceTypeId, string displayName, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ResourceManagementResult.Failure(MissingTenantContext);

        string trimmedName = displayName?.Trim() ?? string.Empty;

        if (trimmedName.Length < 2 || trimmedName.Length > 160)
            return ResourceManagementResult.Failure(InvalidRequest);

        bool resourceTypeExists = await dbContext.ResourceTypes
            .AnyAsync(entity => entity.Id == resourceTypeId, cancellationToken);

        if (!resourceTypeExists)
            return ResourceManagementResult.Failure(ResourceTypeNotFound);

        DateTimeOffset now = timeProvider.GetUtcNow();
        Resource resource = Resource.Create(tenantId, branchId, resourceTypeId, trimmedName);

        dbContext.Resources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "resources.resource.created",
            $$"""{"tenantId":"{{tenantId}}","resourceId":"{{resource.Id}}","branchId":"{{branchId}}"}""",
            now), cancellationToken);

        return ResourceManagementResult.Success(ToView(resource));
    }

    public async Task<ResourceManagementResult> RenameAsync(Guid actorUserAccountId, Guid resourceId, string displayName, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ResourceManagementResult.Failure(MissingTenantContext);

        string trimmedName = displayName?.Trim() ?? string.Empty;

        if (trimmedName.Length < 2 || trimmedName.Length > 160)
            return ResourceManagementResult.Failure(InvalidRequest);

        Resource? resource = await dbContext.Resources
            .FirstOrDefaultAsync(entity => entity.Id == resourceId, cancellationToken);

        if (resource is null)
            return ResourceManagementResult.Failure(ResourceNotFound);

        resource.Rename(trimmedName);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "resources.resource.renamed",
            $$"""{"tenantId":"{{tenantId}}","resourceId":"{{resourceId}}"}""",
            now), cancellationToken);

        return ResourceManagementResult.Success(ToView(resource));
    }

    public async Task<ResourceManagementResult> MarkOutOfServiceAsync(Guid actorUserAccountId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ResourceManagementResult.Failure(MissingTenantContext);

        Resource? resource = await dbContext.Resources
            .FirstOrDefaultAsync(entity => entity.Id == resourceId, cancellationToken);

        if (resource is null)
            return ResourceManagementResult.Failure(ResourceNotFound);

        resource.MarkOutOfService();
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "resources.resource.out-of-service",
            $$"""{"tenantId":"{{tenantId}}","resourceId":"{{resourceId}}"}""",
            now), cancellationToken);

        return ResourceManagementResult.Success(ToView(resource));
    }

    public async Task<ResourceManagementResult> RestoreAsync(Guid actorUserAccountId, Guid resourceId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ResourceManagementResult.Failure(MissingTenantContext);

        Resource? resource = await dbContext.Resources
            .FirstOrDefaultAsync(entity => entity.Id == resourceId, cancellationToken);

        if (resource is null)
            return ResourceManagementResult.Failure(ResourceNotFound);

        resource.Restore();
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "resources.resource.restored",
            $$"""{"tenantId":"{{tenantId}}","resourceId":"{{resourceId}}"}""",
            now), cancellationToken);

        return ResourceManagementResult.Success(ToView(resource));
    }

    private static ResourceView ToView(Resource resource)
        => new(resource.Id, resource.ResourceTypeId, resource.DisplayName, resource.Status.ToString());
}
