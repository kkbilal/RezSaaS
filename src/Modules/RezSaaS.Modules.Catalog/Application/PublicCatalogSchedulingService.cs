using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Catalog.Domain;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;

namespace RezSaaS.Modules.Catalog.Application;

public sealed class PublicCatalogSchedulingService
{
    private readonly CatalogDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicCatalogSchedulingService(
        CatalogDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<PublicServiceVariantSchedulingView>> GetVariantsAsync(
        IReadOnlyCollection<Guid> serviceVariantIds,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null || serviceVariantIds.Count == 0)
        {
            return [];
        }

        Guid[] distinctVariantIds = serviceVariantIds
            .Distinct()
            .ToArray();

        List<PublicServiceVariantSchedulingSeed> variants = await dbContext.ServiceVariants
            .AsNoTracking()
            .Where(variant => distinctVariantIds.Contains(variant.Id))
            .Join(
                dbContext.Services.AsNoTracking().Where(service => service.Status == ServiceStatus.Active),
                variant => variant.ServiceId,
                service => service.Id,
                (variant, service) => new PublicServiceVariantSchedulingSeed(
                    variant.Id,
                    service.Id,
                    service.Name,
                    variant.Name,
                    variant.DurationMinutes,
                    variant.PriceAmount,
                    variant.CurrencyCode,
                    variant.RequiredResourceTypeId))
            .ToListAsync(cancellationToken);

        Guid[] variantIds = variants.Select(entity => entity.Id).ToArray();
        List<PublicServiceVariantRequiredSkillSeed> requiredSkills =
            await dbContext.ServiceRequiredSkills
                .AsNoTracking()
                .Where(entity => variantIds.Contains(entity.ServiceVariantId))
                .Select(entity => new PublicServiceVariantRequiredSkillSeed(
                    entity.ServiceVariantId,
                    entity.SkillId))
                .ToListAsync(cancellationToken);
        ILookup<Guid, Guid> requiredSkillsByVariantId = requiredSkills
            .ToLookup(
                entity => entity.ServiceVariantId,
                entity => entity.SkillId);

        return variants
            .Select(variant => new PublicServiceVariantSchedulingView(
                variant.Id,
                variant.ServiceId,
                variant.ServiceName,
                variant.VariantName,
                variant.DurationMinutes,
                variant.PriceAmount,
                variant.CurrencyCode,
                variant.RequiredResourceTypeId,
                requiredSkillsByVariantId[variant.Id].Distinct().ToArray()))
            .ToArray();
    }

    private sealed record PublicServiceVariantSchedulingSeed(
        Guid Id,
        Guid ServiceId,
        string ServiceName,
        string VariantName,
        int DurationMinutes,
        decimal PriceAmount,
        string CurrencyCode,
        Guid? RequiredResourceTypeId);

    private sealed record PublicServiceVariantRequiredSkillSeed(
        Guid ServiceVariantId,
        Guid SkillId);
}
