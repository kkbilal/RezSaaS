using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Reviews;
using RezSaaS.Modules.Reviews.Domain;
using RezSaaS.Modules.Reviews.Infrastructure.Persistence;

namespace RezSaaS.Modules.Reviews.Application;

public sealed class ModerateReviewService
{
    private const string InvalidDecision = "REVIEW_INVALID_DECISION";
    private const string NotFound = "REVIEW_NOT_FOUND";
    private const string TenantMismatch = "REVIEW_TENANT_MISMATCH";

    private readonly ReviewsDbContext dbContext;
    private readonly IBusinessRatingSummarySink ratingSummarySink;
    private readonly TimeProvider timeProvider;

    public ModerateReviewService(
        ReviewsDbContext dbContext,
        IBusinessRatingSummarySink ratingSummarySink,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.ratingSummarySink = ratingSummarySink;
        this.timeProvider = timeProvider;
    }

    public async Task<ReviewOperationResult> ModerateAsync(
        ModerateReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        string decision = (command.Decision ?? string.Empty).Trim().ToLowerInvariant();

        if (decision is not ("publish" or "reject"))
        {
            return ReviewOperationResult.Failure(InvalidDecision);
        }

        Review? review = await dbContext.Reviews.SingleOrDefaultAsync(
            entity => entity.Id == command.ReviewId,
            cancellationToken);

        if (review is null)
        {
            return ReviewOperationResult.Failure(NotFound);
        }

        if (review.TenantId != command.TenantId)
        {
            // Tenant boundary: do not leak existence -> treat as not found.
            return ReviewOperationResult.Failure(NotFound);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        bool becamePublished;
        try
        {
            if (decision == "publish")
            {
                becamePublished = review.Status != ReviewStatus.Published;
                review.Publish(command.ActorUserAccountId, now);
            }
            else
            {
                bool wasPublished = review.Status == ReviewStatus.Published;
                review.Reject(command.ActorUserAccountId, now, command.ModerationNote);
                becamePublished = false;

                // If a previously published review is being rejected, the summary must be
                // recomputed so it no longer counts.
                if (wasPublished)
                {
                    becamePublished = true;
                }
            }
        }
        catch (InvalidOperationException)
        {
            return ReviewOperationResult.Failure("REVIEW_INVALID_STATE");
        }
        catch (ArgumentException)
        {
            return ReviewOperationResult.Failure("REVIEW_VALIDATION");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Recompute the business rating summary whenever a review transitions into or
        // out of the published state.
        if (becamePublished || decision == "publish")
        {
            await ratingSummarySink.RecomputeAsync(review.TenantId, review.BusinessId, cancellationToken);
        }
        else if (decision == "reject")
        {
            // For pending -> rejected there is no summary impact, but recompute is cheap
            // and keeps the aggregate consistent regardless of previous state.
            await ratingSummarySink.RecomputeAsync(review.TenantId, review.BusinessId, cancellationToken);
        }

        return ReviewOperationResult.Success(
            new ReviewView(
                review.Id,
                review.BusinessId,
                review.BranchId,
                review.AppointmentId,
                review.Rating,
                review.Comment,
                review.Status.ToString(),
                review.CreatedAtUtc,
                review.ModeratedAtUtc,
                CustomerDisplayName: string.Empty,
                ServiceNames: Array.Empty<string>()));
    }
}