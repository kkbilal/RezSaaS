namespace RezSaaS.Modules.Reviews.Domain;

/// <summary>
/// Lifecycle of a customer review.
/// </summary>
public enum ReviewStatus
{
    /// <summary>
    /// Customer submitted the review; waiting for business moderation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Business approved/published the review; visible on public profile.
    /// </summary>
    Published = 1,

    /// <summary>
    /// Business or platform rejected the review; not visible publicly.
    /// </summary>
    Rejected = 2
}