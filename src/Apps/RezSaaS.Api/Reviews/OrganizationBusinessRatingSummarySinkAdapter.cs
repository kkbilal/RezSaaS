using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Reviews;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;
using RezSaaS.Modules.Reviews.Domain;
using RezSaaS.Modules.Reviews.Infrastructure.Persistence;
using BusinessEntity = RezSaaS.Modules.Organization.Domain.Business;

namespace RezSaaS.Api.Reviews;

/// <summary>
/// Composition-root adapter that implements the Reviews cross-module contract
/// <see cref="IBusinessRatingSummarySink"/>. Recomputes the published-review aggregate
/// from the Reviews module and writes it into the Organization module's Business entity.
/// </summary>
public sealed class OrganizationBusinessRatingSummarySinkAdapter : IBusinessRatingSummarySink
{
    private readonly OrganizationDbContext organizationDbContext;
    private readonly ReviewsDbContext reviewsDbContext;

    public OrganizationBusinessRatingSummarySinkAdapter(
        OrganizationDbContext organizationDbContext,
        ReviewsDbContext reviewsDbContext)
    {
        this.organizationDbContext = organizationDbContext;
        this.reviewsDbContext = reviewsDbContext;
    }

    public async Task RecomputeAsync(
        Guid tenantId,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var summary = await reviewsDbContext.Reviews
            .AsNoTracking()
            .Where(review => review.TenantId == tenantId
                && review.BusinessId == businessId
                && review.Status == ReviewStatus.Published)
            .GroupBy(review => review.BusinessId)
            .Select(group => new
            {
                Average = group.Average(review => (double)review.Rating),
                Count = group.Count(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        decimal ratingAverage = summary is null ? 0m : Math.Round((decimal)summary.Average, 2);
        int reviewCount = summary?.Count ?? 0;

        BusinessEntity? business = await organizationDbContext.Businesses
            .Where(business => business.TenantId == tenantId && business.Id == businessId)
            .FirstOrDefaultAsync(cancellationToken);

        if (business is null)
        {
            return;
        }

        business.UpdateRatingSummary(ratingAverage, reviewCount);
        await organizationDbContext.SaveChangesAsync(cancellationToken);
    }
}