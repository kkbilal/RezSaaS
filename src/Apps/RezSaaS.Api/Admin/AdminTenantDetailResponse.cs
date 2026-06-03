namespace RezSaaS.Api.Admin;

public sealed record AdminTenantDetailResponse(
    Guid TenantId,
    string Slug,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    IReadOnlyCollection<AdminTenantMembershipResponse> Memberships);
