namespace RezSaaS.Api.Customer;

public sealed record CustomerSanctionResponse(
    Guid SanctionId,
    string Type,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    DateTimeOffset? RevokedAtUtc,
    bool IsActive);
