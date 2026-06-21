using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Reviews.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.PublicApi;

/// <summary>
/// Composes a public review summary for a business profile page.
/// Tenant context is temporarily set from the verified business slug (composition root responsibility),
/// then restored to the previous value, following the same pattern as PublicBusinessProfileComposer.
/// </summary>
public sealed class PublicReviewComposer
{
    private readonly PublicBusinessDirectoryService businessDirectoryService;
    private readonly TenantLifecycleQueryService tenantLifecycleQueryService;
    private readonly PublicReviewQueryService reviewQueryService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public PublicReviewComposer(
        PublicBusinessDirectoryService businessDirectoryService,
        TenantLifecycleQueryService tenantLifecycleQueryService,
        PublicReviewQueryService reviewQueryService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.businessDirectoryService = businessDirectoryService;
        this.tenantLifecycleQueryService = tenantLifecycleQueryService;
        this.reviewQueryService = reviewQueryService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<PublicReviewSummaryResponse?> GetAsync(
        string slug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(slug, cancellationToken);

        if (business is null
            || !await tenantLifecycleQueryService.IsActiveAsync(business.TenantId, cancellationToken))
        {
            return null;
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            PublicReviewSummaryView summary =
                await reviewQueryService.GetAsync(business.BusinessId, page, pageSize, cancellationToken);

            return new PublicReviewSummaryResponse(
                summary.RatingAverage,
                summary.ReviewCount,
                summary.Reviews
                    .Select(review => new PublicReviewResponse(
                        review.Id,
                        review.Rating,
                        review.Comment,
                        review.CustomerDisplayName,
                        review.CreatedAtUtc))
                    .ToArray());
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }
}

public sealed record PublicReviewSummaryResponse(
    decimal? AverageRating,
    int TotalCount,
    IReadOnlyCollection<PublicReviewResponse> Reviews);

public sealed record PublicReviewResponse(
    Guid Id,
    int Rating,
    string Comment,
    string CustomerDisplayName,
    DateTimeOffset CreatedAtUtc);