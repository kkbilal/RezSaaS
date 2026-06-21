namespace RezSaaS.Modules.Reviews.Application;

public sealed record CreateReviewCommand(
    Guid TenantId,
    Guid CustomerUserAccountId,
    Guid AppointmentId,
    int Rating,
    string Comment);

public sealed record ModerateReviewCommand(
    Guid TenantId,
    Guid ActorUserAccountId,
    Guid ReviewId,
    string Decision, // "publish" | "reject"
    string? ModerationNote);

public sealed record ReviewOperationResult(
    bool Succeeded,
    string? ErrorCode,
    ReviewView? Review)
{
    public static ReviewOperationResult Success(ReviewView review) =>
        new(true, null, review);

    public static ReviewOperationResult Failure(string errorCode) =>
        new(false, errorCode, null);
}

public sealed record ReviewView(
    Guid Id,
    Guid BusinessId,
    Guid BranchId,
    Guid AppointmentId,
    int Rating,
    string Comment,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModeratedAtUtc,
    string CustomerDisplayName,
    IReadOnlyCollection<string> ServiceNames);

public sealed record PublicReviewSummaryView(
    decimal? RatingAverage,
    int ReviewCount,
    IReadOnlyCollection<PublicReviewView> Reviews);

public sealed record PublicReviewView(
    Guid Id,
    int Rating,
    string Comment,
    string CustomerDisplayName,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<string> ServiceNames);

public sealed record BusinessReviewListItemView(
    Guid Id,
    Guid AppointmentId,
    int Rating,
    string Comment,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModeratedAtUtc,
    string CustomerDisplayName,
    IReadOnlyCollection<string> ServiceNames);