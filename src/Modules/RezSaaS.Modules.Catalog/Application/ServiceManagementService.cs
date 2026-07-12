using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Catalog.Domain;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;

namespace RezSaaS.Modules.Catalog.Application;

public sealed class ServiceManagementService
{
    public const string InvalidRequest = "SERVICE_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string ServiceNotFound = "SERVICE_NOT_FOUND";
    public const string NameConflict = "SERVICE_NAME_CONFLICT";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly CatalogDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public ServiceManagementService(
        CatalogDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor,
        IAuditLogRecorder auditLogRecorder,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
        this.auditLogRecorder = auditLogRecorder;
        this.timeProvider = timeProvider;
    }

    public async Task<ServiceManagementResult> ListAsync(CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return ServiceManagementResult.Failure(MissingTenantContext);

        List<ServiceView> services = await dbContext.Services
            .AsNoTracking()
            .OrderBy(entity => entity.Name)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return ServiceManagementResult.SuccessList(services);
    }

    public async Task<ServiceManagementResult> GetByIdAsync(
        Guid serviceId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return ServiceManagementResult.Failure(MissingTenantContext);

        ServiceView? service = await dbContext.Services
            .AsNoTracking()
            .Where(entity => entity.Id == serviceId)
            .Select(entity => ToView(entity))
            .FirstOrDefaultAsync(cancellationToken);

        return service is not null
            ? ServiceManagementResult.Success(service)
            : ServiceManagementResult.Failure(ServiceNotFound);
    }

    public async Task<ServiceManagementResult> CreateAsync(
        CreateServiceCommand command, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ServiceManagementResult.Failure(MissingTenantContext);

        string trimmedName = command.Name?.Trim() ?? string.Empty;
        string trimmedCategory = command.CategoryKey?.Trim() ?? string.Empty;

        if (trimmedName.Length < 2 || trimmedName.Length > 160 || trimmedCategory.Length < 2 || trimmedCategory.Length > 80)
            return ServiceManagementResult.Failure(InvalidRequest);

        string upperName = trimmedName.ToUpperInvariant();
        bool nameExists = await dbContext.Services
            .AnyAsync(entity => entity.NormalizedName == upperName, cancellationToken);

        if (nameExists)
            return ServiceManagementResult.Failure(NameConflict);

        DateTimeOffset now = timeProvider.GetUtcNow();
        Service service = Service.Create(tenantId, trimmedName, trimmedCategory, now);

        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, command.ActorUserAccountId, "catalog.service.created",
            $$"""{"tenantId":"{{tenantId}}","serviceId":"{{service.Id}}","name":"{{trimmedName}}"}""",
            now), cancellationToken);

        return ServiceManagementResult.Success(ToView(service));
    }

    public async Task<ServiceManagementResult> UpdateAsync(
        UpdateServiceCommand command, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ServiceManagementResult.Failure(MissingTenantContext);

        string trimmedName = command.Name?.Trim() ?? string.Empty;
        string trimmedCategory = command.CategoryKey?.Trim() ?? string.Empty;

        if (trimmedName.Length < 2 || trimmedName.Length > 160 || trimmedCategory.Length < 2 || trimmedCategory.Length > 80)
            return ServiceManagementResult.Failure(InvalidRequest);

        Service? service = await dbContext.Services
            .FirstOrDefaultAsync(entity => entity.Id == command.ServiceId, cancellationToken);

        if (service is null)
            return ServiceManagementResult.Failure(ServiceNotFound);

        string upperName = trimmedName.ToUpperInvariant();
        bool nameExists = await dbContext.Services
            .AnyAsync(entity => entity.NormalizedName == upperName
                && entity.Id != command.ServiceId, cancellationToken);

        if (nameExists)
            return ServiceManagementResult.Failure(NameConflict);

        service.Rename(trimmedName);
        service.UpdateCategory(trimmedCategory);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, command.ActorUserAccountId, "catalog.service.updated",
            $$"""{"tenantId":"{{tenantId}}","serviceId":"{{service.Id}}"}""",
            now), cancellationToken);

        return ServiceManagementResult.Success(ToView(service));
    }

    public async Task<ServiceManagementResult> ArchiveAsync(
        Guid actorUserAccountId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ServiceManagementResult.Failure(MissingTenantContext);

        Service? service = await dbContext.Services
            .FirstOrDefaultAsync(entity => entity.Id == serviceId, cancellationToken);

        if (service is null)
            return ServiceManagementResult.Failure(ServiceNotFound);

        // BUG FIX: burada `dbContext.Services.Remove(service)` VARDI -- yani "arsivle" adli
        // uc aslinda KALICI SILME yapiyordu. Domain'de calisan bir Archive() metodu vardi
        // (Status = Archived) ama HIC CAGRILMIYORDU. Ustelik audit kaydi "catalog.service.archived"
        // diyordu; hem kullaniciya hem denetim gunlugune yalan soyleniyordu.
        // (Personel Rename bug'inin birebir aynisi: domain metodu var, servis cagirmiyor.)
        //
        // Ayrica "varyanti varsa arsivleme" (SERVICE_HAS_VARIANTS -> 409) engeli KALDIRILDI.
        // O engel hard-delete'in FK artigiydi. Fiyat ve sure ZATEN varyantta yasiyor, yani
        // GERCEK HER HIZMETIN varyanti var -- engel, arsivlemeyi pratikte TAMAMEN kullanilamaz
        // kiliyordu. Soft archive'da gereksiz: varyantlar DB'de kalir, hizmet Archived olur.
        //
        // Arsivlenmis hizmet public yuzeylerden OTOMATIK dusuyor; altyapi zaten hazirdi:
        //   PublicCatalogMenuService       -> Where(Status == Active)   (profil menusu)
        //   PublicCatalogSchedulingService -> Where(Status == Active)   (SLOT ARAMA)
        // Mevcut randevular etkilenmez: isim/fiyat line'larda snapshot'lanmis durumda.
        service.Archive();
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "catalog.service.archived",
            $$"""{"tenantId":"{{tenantId}}","serviceId":"{{serviceId}}"}""",
            now), cancellationToken);

        return ServiceManagementResult.Success(ToView(service));
    }

    private static ServiceView ToView(Service service)
        => new(service.Id, service.Name, service.CategoryKey, service.Status.ToString(), service.CreatedAtUtc);
}
