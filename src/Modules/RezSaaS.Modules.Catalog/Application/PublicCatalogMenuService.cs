using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Catalog.Domain;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;

namespace RezSaaS.Modules.Catalog.Application;

public sealed class PublicCatalogMenuService
{
    private readonly CatalogDbContext dbContext;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicCatalogMenuService(
        CatalogDbContext dbContext,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.dbContext = dbContext;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<IReadOnlyCollection<PublicServiceMenuView>> GetMenuAsync(
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is null)
        {
            return [];
        }

        List<Service> services = await dbContext.Services
            .AsNoTracking()
            .Where(entity => entity.Status == ServiceStatus.Active)
            .OrderBy(entity => entity.Name)
            .ToListAsync(cancellationToken);
        Guid[] serviceIds = services
            .Select(entity => entity.Id)
            .ToArray();

        List<ServiceVariant> variants = await dbContext.ServiceVariants
            .AsNoTracking()
            .Where(entity => serviceIds.Contains(entity.ServiceId))
            .OrderBy(entity => entity.Name)
            .ToListAsync(cancellationToken);
        ILookup<Guid, ServiceVariant> variantsByServiceId = variants.ToLookup(entity => entity.ServiceId);

        return services
            .Select(service => new PublicServiceMenuView(
                service.Id,
                service.Name,
                service.CategoryKey,
                variantsByServiceId[service.Id]
                    .Select(variant => new PublicServiceVariantView(
                        variant.Id,
                        variant.Name,
                        variant.DurationMinutes,
                        variant.PriceAmount,
                        variant.CurrencyCode,
                        variant.RequiredResourceTypeId))
                    .ToArray()))
            .ToArray();
    }
}
