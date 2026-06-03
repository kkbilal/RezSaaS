namespace RezSaaS.Api.Admin;

public sealed record AdminTenantListItemResponse(
    Guid TenantId,
    string Slug,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    int ActiveMembershipCount);
