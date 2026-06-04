namespace RezSaaS.BuildingBlocks.Abuse;

public sealed record UserBookingRestriction(
    bool IsRestricted,
    string? RestrictionCode,
    DateTimeOffset? EndsAtUtc)
{
    public static UserBookingRestriction None { get; } =
        new(false, RestrictionCode: null, EndsAtUtc: null);

    public static UserBookingRestriction Restricted(
        string restrictionCode,
        DateTimeOffset? endsAtUtc)
    {
        return new UserBookingRestriction(
            true,
            restrictionCode,
            endsAtUtc);
    }
}
