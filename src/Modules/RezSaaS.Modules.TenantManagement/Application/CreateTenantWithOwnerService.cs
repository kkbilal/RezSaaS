using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.Modules.TenantManagement.Application;

public sealed class CreateTenantWithOwnerService
{
    private const string InvalidRequest = "TENANT_PROVISIONING_INVALID";
    private const string SlugAlreadyExists = "TENANT_SLUG_ALREADY_EXISTS";

    private readonly TenantManagementDbContext dbContext;
    private readonly TimeProvider timeProvider;

    public CreateTenantWithOwnerService(
        TenantManagementDbContext dbContext,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.timeProvider = timeProvider;
    }

    public async Task<CreateTenantWithOwnerResult> CreateAsync(
        CreateTenantWithOwnerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ActorUserAccountId == Guid.Empty
            || command.OwnerUserAccountId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Slug)
            || string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return CreateTenantWithOwnerResult.Failure(InvalidRequest);
        }

        string normalizedSlug = command.Slug.Trim().ToUpperInvariant();

        if (await dbContext.Tenants
            .AsNoTracking()
            .AnyAsync(entity => entity.NormalizedSlug == normalizedSlug, cancellationToken))
        {
            return CreateTenantWithOwnerResult.Failure(SlugAlreadyExists);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        Tenant tenant;

        try
        {
            tenant = Tenant.Create(
                command.Slug,
                command.DisplayName,
                now);
            tenant.AddMembership(
                command.OwnerUserAccountId,
                TenantMembershipRole.BusinessOwner,
                now);
        }
        catch (ArgumentException)
        {
            return CreateTenantWithOwnerResult.Failure(InvalidRequest);
        }

        dbContext.Tenants.Add(tenant);
        dbContext.AuditLogEntries.Add(
            TenantAuditLogEntry.Create(
                tenant.Id,
                command.ActorUserAccountId,
                "TenantProvisioned",
                $$"""{"tenantId":"{{tenant.Id}}","ownerUserAccountId":"{{command.OwnerUserAccountId}}"}""",
                now));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return CreateTenantWithOwnerResult.Failure(SlugAlreadyExists);
        }

        return CreateTenantWithOwnerResult.Success(tenant.Id);
    }
}
