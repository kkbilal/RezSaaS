namespace RezSaaS.Modules.TenantManagement.Application;

public sealed record CreateTenantWithOwnerCommand(
    Guid ActorUserAccountId,
    string Slug,
    string DisplayName,
    Guid OwnerUserAccountId);
