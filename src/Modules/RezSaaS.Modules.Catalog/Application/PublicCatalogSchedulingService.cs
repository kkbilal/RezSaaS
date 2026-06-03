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

        return await dbContext.ServiceVariants
            .AsNoTracking()
            .Where(variant => distinctVariantIds.Contains(variant.Id))
            .Join(
                dbContext.Services.AsNoTracking().Where(service => service.Status == ServiceStatus.Active),
                variant => variant.ServiceId,
                service => service.Id,
                (variant, service) => new PublicServiceVariantSchedulingView(
                    variant.Id,
                    service.Id,
                    service.Name,
                    variant.Name,
                    variant.DurationMinutes,
                    variant.PriceAmount,
                    variant.CurrencyCode,
                    variant.RequiredResourceTypeId))
            .ToListAsync(cancellationToken);
    }
}
