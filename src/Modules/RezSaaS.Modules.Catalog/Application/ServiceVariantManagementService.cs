using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Auditing;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Catalog.Domain;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;

namespace RezSaaS.Modules.Catalog.Application;

public sealed class ServiceVariantManagementService
{
    public const string InvalidRequest = "VARIANT_INVALID_REQUEST";
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string ServiceNotFound = "SERVICE_NOT_FOUND";
    public const string VariantNotFound = "VARIANT_NOT_FOUND";
    public const string NameConflict = "VARIANT_NAME_CONFLICT";

    private readonly IAuditLogRecorder auditLogRecorder;
    private readonly CatalogDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public ServiceVariantManagementService(
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

    public async Task<ServiceVariantManagementResult> ListByServiceAsync(
        Guid serviceId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return ServiceVariantManagementResult.Failure(MissingTenantContext);

        List<ServiceVariantView> variants = await dbContext.ServiceVariants
            .AsNoTracking()
            .Where(entity => entity.ServiceId == serviceId)
            .OrderBy(entity => entity.Name)
            .Select(entity => ToView(entity))
            .ToListAsync(cancellationToken);

        return ServiceVariantManagementResult.SuccessList(variants);
    }

    public async Task<ServiceVariantManagementResult> GetByIdAsync(
        Guid variantId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
            return ServiceVariantManagementResult.Failure(MissingTenantContext);

        ServiceVariantView? variant = await dbContext.ServiceVariants
            .AsNoTracking()
            .Where(entity => entity.Id == variantId)
            .Select(entity => ToView(entity))
            .FirstOrDefaultAsync(cancellationToken);

        return variant is not null
            ? ServiceVariantManagementResult.Success(variant)
            : ServiceVariantManagementResult.Failure(VariantNotFound);
    }

    public async Task<ServiceVariantManagementResult> CreateAsync(
        CreateServiceVariantCommand command, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ServiceVariantManagementResult.Failure(MissingTenantContext);

        string trimmedName = command.Name?.Trim() ?? string.Empty;

        if (trimmedName.Length < 2 || trimmedName.Length > 160
            || command.DurationMinutes <= 0 || command.DurationMinutes > 1440
            || command.PriceAmount < 0
            // CreateAsync'te de WHITELIST: eskiden sadece "bos olmasin" deniyordu, yani
            // USD (ya da herhangi bir 3-karakter string) ile varyant yaratilabiliyordu ve
            // ayni isletmenin katalogu KARISIK PARA BIRIMINE dusuyordu.
            || !ServiceVariant.IsSupportedCurrency(command.CurrencyCode))
            return ServiceVariantManagementResult.Failure(InvalidRequest);

        bool serviceExists = await dbContext.Services
            .AnyAsync(entity => entity.Id == command.ServiceId, cancellationToken);

        if (!serviceExists)
            return ServiceVariantManagementResult.Failure(ServiceNotFound);

        string upperName = trimmedName.ToUpperInvariant();
        bool nameExists = await dbContext.ServiceVariants
            .AnyAsync(entity => entity.ServiceId == command.ServiceId
                && entity.NormalizedName == upperName, cancellationToken);

        if (nameExists)
            return ServiceVariantManagementResult.Failure(NameConflict);

        DateTimeOffset now = timeProvider.GetUtcNow();
        ServiceVariant variant = ServiceVariant.Create(
            tenantId, command.ServiceId, trimmedName,
            command.DurationMinutes, command.PriceAmount,
            command.CurrencyCode.Trim().ToUpperInvariant(), now,
            command.RequiredResourceTypeId);

        dbContext.ServiceVariants.Add(variant);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, command.ActorUserAccountId, "catalog.variant.created",
            $$"""{"tenantId":"{{tenantId}}","variantId":"{{variant.Id}}","serviceId":"{{command.ServiceId}}"}""",
            now), cancellationToken);

        return ServiceVariantManagementResult.Success(ToView(variant));
    }

    public async Task<ServiceVariantManagementResult> UpdateAsync(
        UpdateServiceVariantCommand command, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ServiceVariantManagementResult.Failure(MissingTenantContext);

        string trimmedName = command.Name?.Trim() ?? string.Empty;

        if (trimmedName.Length < 2 || trimmedName.Length > 160
            || command.DurationMinutes <= 0 || command.DurationMinutes > 1440
            || command.PriceAmount < 0
            // Para birimi artik WHITELIST'e karsi dogrulaniyor (eskiden sadece "bos olmasin"
            // deniyordu -> katalog karisik para birimine dusebiliyordu).
            || !ServiceVariant.IsSupportedCurrency(command.CurrencyCode))
            return ServiceVariantManagementResult.Failure(InvalidRequest);

        ServiceVariant? variant = await dbContext.ServiceVariants
            .FirstOrDefaultAsync(entity => entity.Id == command.VariantId
                && entity.ServiceId == command.ServiceId, cancellationToken);

        if (variant is null)
            return ServiceVariantManagementResult.Failure(VariantNotFound);

        string upperName = trimmedName.ToUpperInvariant();
        bool nameExists = await dbContext.ServiceVariants
            .AnyAsync(entity => entity.ServiceId == command.ServiceId
                && entity.NormalizedName == upperName
                && entity.Id != command.VariantId, cancellationToken);

        if (nameExists)
            return ServiceVariantManagementResult.Failure(NameConflict);

        variant.Rename(trimmedName);
        variant.UpdateDuration(command.DurationMinutes);
        // BUG FIX: CurrencyCode dogrulaniyordu ama UYGULANMIYORDU (domain'de metot yoktu).
        // Istek 200 OK donuyor, para birimi degismiyordu -- sozlesme yalan soyluyordu.
        variant.UpdatePricing(command.PriceAmount, command.CurrencyCode);
        variant.UpdateResourceType(command.RequiredResourceTypeId);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, command.ActorUserAccountId, "catalog.variant.updated",
            $$"""{"tenantId":"{{tenantId}}","variantId":"{{variant.Id}}"}""",
            now), cancellationToken);

        return ServiceVariantManagementResult.Success(ToView(variant));
    }

    public async Task<ServiceVariantManagementResult> DeleteAsync(
        Guid actorUserAccountId, Guid variantId, CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
            return ServiceVariantManagementResult.Failure(MissingTenantContext);

        ServiceVariant? variant = await dbContext.ServiceVariants
            .FirstOrDefaultAsync(entity => entity.Id == variantId, cancellationToken);

        if (variant is null)
            return ServiceVariantManagementResult.Failure(VariantNotFound);

        dbContext.ServiceVariants.Remove(variant);
        await dbContext.SaveChangesAsync(cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        await auditLogRecorder.RecordAsync(new AuditLogRecord(
            tenantId, actorUserAccountId, "catalog.variant.deleted",
            $$"""{"tenantId":"{{tenantId}}","variantId":"{{variantId}}"}""",
            now), cancellationToken);

        return ServiceVariantManagementResult.Success(ToView(variant));
    }

    private static ServiceVariantView ToView(ServiceVariant variant)
        => new(variant.Id, variant.ServiceId, variant.Name, variant.DurationMinutes,
            variant.PriceAmount, variant.CurrencyCode, variant.RequiredResourceTypeId, variant.CreatedAtUtc);
}
