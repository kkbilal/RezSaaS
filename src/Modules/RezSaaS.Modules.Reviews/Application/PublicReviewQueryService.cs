using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Reviews;
using RezSaaS.Modules.Reviews.Domain;
using RezSaaS.Modules.Reviews.Infrastructure.Persistence;

namespace RezSaaS.Modules.Reviews.Application;

/// <summary>
/// Public read-side: returns published reviews + summary for a business.
/// Uses tenant-scope through the DbContext query filter but must be invoked with
/// explicit tenant context set from a verified businessSlug (composition root responsibility).
/// </summary>
public sealed class PublicReviewQueryService
{
    private const int MaxPageSize = 50;

    private readonly ReviewsDbContext dbContext;
    private readonly ICustomerDisplayNameResolver customerDisplayNameResolver;

    public PublicReviewQueryService(
        ReviewsDbContext dbContext,
        ICustomerDisplayNameResolver customerDisplayNameResolver)
    {
        this.dbContext = dbContext;
        this.customerDisplayNameResolver = customerDisplayNameResolver;
    }

    public async Task<PublicReviewSummaryView> GetAsync(
        Guid businessId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 10 : pageSize;

        int totalPublished = await dbContext.Reviews
            .Where(entity => entity.BusinessId == businessId && entity.Status == ReviewStatus.Published)
            .CountAsync(cancellationToken);

        decimal? average = totalPublished == 0
            ? null
            : await dbContext.Reviews
                .Where(entity => entity.BusinessId == businessId && entity.Status == ReviewStatus.Published)
                .AverageAsync(entity => (decimal?)entity.Rating, cancellationToken);

        List<Review> reviews = await dbContext.Reviews
            .Where(entity => entity.BusinessId == businessId && entity.Status == ReviewStatus.Published)
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> displayNames = await ResolveDisplayNamesAsync(reviews, cancellationToken);

        List<PublicReviewView> views = reviews
            .Select(entity => new PublicReviewView(
                entity.Id,
                entity.Rating,
                entity.Comment,
                displayNames.TryGetValue(entity.CustomerUserAccountId, out string? name) ? name : "Misafir",
                entity.CreatedAtUtc,
                ServiceNames: Array.Empty<string>()))
            .ToList();

        return new PublicReviewSummaryView(
            average.HasValue ? Math.Round(average.Value, 2) : null,
            totalPublished,
            views);
    }

    private async Task<Dictionary<Guid, string>> ResolveDisplayNamesAsync(
        List<Review> reviews,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> distinctUserIds = reviews.Select(r => r.CustomerUserAccountId).ToHashSet();
        var result = new Dictionary<Guid, string>(distinctUserIds.Count);
        foreach (Guid userId in distinctUserIds)
        {
            string? name = await customerDisplayNameResolver.ResolveAsync(userId, cancellationToken);
            if (name is not null)
            {
                result[userId] = name;
            }
        }

        return result;
    }
}