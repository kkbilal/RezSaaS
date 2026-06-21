using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Resources.Domain;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;

namespace RezSaaS.Modules.Resources.Application;

public sealed class ResourceTypeManagementService
{
    public const string InvalidRequest = "RESOURCE_TYPE_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string ResourceTypeNotFound = "RESOURCE_TYPE_NOT_FOUND";
    public const string KeyConflict = "RESOURCE_TYPE_KEY_CONFLICT";
    public const string ResourceTypeInUse = "RESOURCE_TYPE_IN_USE";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly ResourcesDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public ResourceTypeManagementService(
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

    public async Task<ResourceTypeManagementResult> ListAsync(CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return ResourceTypeManagementResult.Failure(MissingTenantContext);

        List<ResourceTypeView> types = await dbContext.ResourceTypes
            .AsNoTracking()
            .OrderBy(entity => entity.Key)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return ResourceTypeManagementResult.SuccessList(types);
    }

    public async Task<ResourceTypeManagementResult> CreateAsync(Guid actorUserAccountId, string key, string displayName, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ResourceTypeManagementResult.Failure(MissingTenantContext);

        string trimmedKey = key?.Trim() ?? string.Empty;
        string trimmedDisplayName = displayName?.Trim() ?? string.Empty;

        if (trimmedKey.Length < 2 || trimmedKey.Length > 80 || trimmedDisplayName.Length < 2 || trimmedDisplayName.Length > 160)
            return ResourceTypeManagementResult.Failure(InvalidRequest);

        string upperKey = trimmedKey.ToUpperInvariant();
        bool keyExists = await dbContext.ResourceTypes
            .AnyAsync(entity => entity.NormalizedKey == upperKey, cancellationToken);

        if (keyExists)
            return ResourceTypeManagementResult.Failure(KeyConflict);

        DateTimeOffset now = timeProvider.GetUtcNow();
        ResourceType resourceType = ResourceType.Create(tenantId, trimmedKey, trimmedDisplayName);

        dbContext.ResourceTypes.Add(resourceType);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "resources.resource-type.created",
            $$"""{"tenantId":"{{tenantId}}","resourceTypeId":"{{resourceType.Id}}","key":"{{trimmedKey}}"}""",
            now), cancellationToken);

        return ResourceTypeManagementResult.Success(ToView(resourceType));
    }

    public async Task<ResourceTypeManagementResult> DeleteAsync(Guid actorUserAccountId, Guid resourceTypeId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ResourceTypeManagementResult.Failure(MissingTenantContext);

        ResourceType? resourceType = await dbContext.ResourceTypes
            .FirstOrDefaultAsync(entity => entity.Id == resourceTypeId, cancellationToken);

        if (resourceType is null)
            return ResourceTypeManagementResult.Failure(ResourceTypeNotFound);

        bool hasResources = await dbContext.Resources
            .AnyAsync(entity => entity.ResourceTypeId == resourceTypeId, cancellationToken);

        if (hasResources)
            return ResourceTypeManagementResult.Failure(ResourceTypeInUse);

        dbContext.ResourceTypes.Remove(resourceType);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "resources.resource-type.deleted",
            $$"""{"tenantId":"{{tenantId}}","resourceTypeId":"{{resourceTypeId}}"}""",
            now), cancellationToken);

        return ResourceTypeManagementResult.Success(ToView(resourceType));
    }

    private static ResourceTypeView ToView(ResourceType resourceType)
        => new(resourceType.Id, resourceType.Key, resourceType.DisplayName);
}
