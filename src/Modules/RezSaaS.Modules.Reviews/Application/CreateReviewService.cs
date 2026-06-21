using Microsoft.EntityFrameworkCore;
using RezSaaS.BuildingBlocks.Reviews;
using RezSaaS.Modules.Reviews.Domain;
using RezSaaS.Modules.Reviews.Infrastructure.Persistence;

namespace RezSaaS.Modules.Reviews.Application;

public sealed class CreateReviewService
{
    private const string AppointmentNotCompleted = "REVIEW_APPOINTMENT_NOT_COMPLETED";
    private const string AppointmentNotFound = "REVIEW_APPOINTMENT_NOT_FOUND";
    private const string DuplicateReview = "REVIEW_DUPLICATE";

    private readonly ReviewsDbContext dbContext;
    private readonly ICompletedAppointmentLookup completedAppointmentLookup;
    private readonly TimeProvider timeProvider;

    public CreateReviewService(
        ReviewsDbContext dbContext,
        ICompletedAppointmentLookup completedAppointmentLookup,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.completedAppointmentLookup = completedAppointmentLookup;
        this.timeProvider = timeProvider;
    }

    public async Task<ReviewOperationResult> CreateAsync(
        CreateReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        CompletedAppointmentSnapshot? snapshot = await completedAppointmentLookup.GetAsync(
            command.TenantId,
            command.AppointmentId,
            command.CustomerUserAccountId,
            cancellationToken);

        if (snapshot is null)
        {
            return ReviewOperationResult.Failure(AppointmentNotFound);
        }

        if (snapshot.CompletedAtUtc == DateTimeOffset.MinValue)
        {
            return ReviewOperationResult.Failure(AppointmentNotCompleted);
        }

        bool exists = await dbContext.Reviews.AnyAsync(
            entity => entity.AppointmentId == command.AppointmentId,
            cancellationToken);

        if (exists)
        {
            return ReviewOperationResult.Failure(DuplicateReview);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        Review review = Review.Create(
            command.TenantId,
            snapshot.BusinessId,
            snapshot.BranchId,
            command.AppointmentId,
            command.CustomerUserAccountId,
            command.Rating,
            command.Comment,
            now);

        dbContext.Reviews.Add(review);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique constraint on (TenantId, AppointmentId) may have raced.
            return ReviewOperationResult.Failure(DuplicateReview);
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
                ServiceNames: snapshot.ServiceNameSnapshots));
    }
}