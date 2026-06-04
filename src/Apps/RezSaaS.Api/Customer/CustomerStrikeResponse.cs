namespace RezSaaS.Api.Customer;

public sealed record CustomerStrikeResponse(
    Guid StrikeId,
    string ReasonCode,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc);
