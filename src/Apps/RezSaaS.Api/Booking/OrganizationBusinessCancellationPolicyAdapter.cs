using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Booking;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;

namespace RezSaaS.Api.Booking;

/// <summary>
/// Composition-root adapter: Booking modulunun <see cref="IBusinessCancellationPolicyLookup"/>
/// sozlesmesini Organization modulunun Business kaydindan besler.
/// </summary>
/// <remarks>
/// Booking, Organization'a DOGRUDAN referans veremez (ModuleDependencyTests moduller arasi
/// referansi yasakliyor). Bu yuzden sozlesme BuildingBlocks'ta, uygulamasi burada --
/// Reviews modulunun adapter'leriyle ayni kalip.
/// </remarks>
public sealed class OrganizationBusinessCancellationPolicyAdapter : IBusinessCancellationPolicyLookup
{
    private readonly OrganizationDbContext organizationDbContext;

    public OrganizationBusinessCancellationPolicyAdapter(OrganizationDbContext organizationDbContext)
    {
        this.organizationDbContext = organizationDbContext;
    }

    public async Task<BusinessCancellationPolicy?> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters: bu cagri musteri baglaminda yapiliyor ve musterinin tenant
        // context'i yok. Tenant'i ACIKCA filtreliyoruz -- global filtreye guvenmiyoruz.
        int? cutoffHours = await organizationDbContext.Businesses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(entity => entity.TenantId == tenantId
                && entity.Status == BusinessStatus.Active)
            .Select(entity => (int?)entity.CancellationCutoffHours)
            .FirstOrDefaultAsync(cancellationToken);

        return cutoffHours is null
            ? null
            : new BusinessCancellationPolicy(cutoffHours.Value);
    }
}
