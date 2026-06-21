namespace RezSaaS.BuildingBlocks.Reviews;

/// <summary>
/// Cross-module contract implemented by the Organization module.
/// Reviews module calls this after publishing/rejecting a review to update
/// the business's <c>RatingAverage</c> and <c>ReviewCount</c>.
/// </summary>
public interface IBusinessRatingSummarySink
{
    /// <summary>
    /// Recomputes and persists the rating summary (average + count of published reviews)
    /// for the given business within the given tenant.
    /// </summary>
    Task RecomputeAsync(
        Guid tenantId,
        Guid businessId,
        CancellationToken cancellationToken = default);
}