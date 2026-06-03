using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record TenantListItemView(
    Guid Id,
    string Slug,
    string DisplayName,
    TenantStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    int ActiveMembershipCount);
