using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Reviews;
using RezSaaS.Modules.Reviews.Domain;
using RezSaaS.Modules.Reviews.Infrastructure.Persistence;

namespace RezSaaS.Modules.Reviews.Application;

/// <summary>
/// Business-panel read-side: returns all reviews (any status) for moderation.
/// Tenant-scoped via DbContext query filter.
/// </summary>
public sealed class BusinessReviewQueryService
{
    private const int MaxPageSize = 100;

    private readonly ReviewsDbContext dbContext;
    private readonly ICustomerDisplayNameResolver customerDisplayNameResolver;

    public BusinessReviewQueryService(
        ReviewsDbContext dbContext,
        ICustomerDisplayNameResolver customerDisplayNameResolver)
    {
        this.dbContext = dbContext;
        this.customerDisplayNameResolver = customerDisplayNameResolver;
    }

    public async Task<BusinessReviewListResult> ListAsync(
        Guid businessId,
        ReviewStatus? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        IQueryable<Review> query = dbContext.Reviews
            .Where(entity => entity.BusinessId == businessId);

        if (statusFilter is not null)
        {
            query = query.Where(entity => entity.Status == statusFilter);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<Review> reviews = await query
            .OrderByDescending(entity => entity.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, string> displayNames = await ResolveDisplayNamesAsync(reviews, cancellationToken);

        List<BusinessReviewListItemView> views = reviews
            .Select(entity => new BusinessReviewListItemView(
                entity.Id,
                entity.AppointmentId,
                entity.Rating,
                entity.Comment,
                entity.Status.ToString(),
                entity.CreatedAtUtc,
                entity.ModeratedAtUtc,
                displayNames.TryGetValue(entity.CustomerUserAccountId, out string? name) ? name : "Bilinmeyen",
                ServiceNames: Array.Empty<string>()))
            .ToList();

        return new BusinessReviewListResult(totalCount, page, pageSize, views);
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

public sealed record BusinessReviewListResult(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyCollection<BusinessReviewListItemView> Reviews);