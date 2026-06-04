namespace RezSaaS.BuildingBlocks.Abuse;

public interface IUserBookingRestrictionEvaluator
{
    Task<UserBookingRestriction> EvaluateAsync(
        Guid userAccountId,
        DateTimeOffset evaluatedAtUtc,
        CancellationToken cancellationToken = default);
}
