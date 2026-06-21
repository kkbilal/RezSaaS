using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RezSaaS.BuildingBlocks.Tenancy;

namespace RezSaaS.Api.Configuration;

/// <summary>
/// EF Core interceptor that automatically stamps tenant-scoped entities with the current tenant ID.
/// This prevents cross-tenant data leakage at the database level during INSERT operations.
/// </summary>
public sealed class TenantStampingInterceptor : SaveChangesInterceptor
{
    private readonly ITenantAccessor _tenantAccessor;

    public TenantStampingInterceptor(ITenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTenantStamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantStamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyTenantStamp(DbContext? context)
    {
        if (context == null)
            return;

        var tenantId = _tenantAccessor.TenantId;
        if (!tenantId.HasValue)
            return;

        // Get all entities that implement ITenantScoped
        var tenantScopedEntries = context.ChangeTracker.Entries<ITenantScoped>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in tenantScopedEntries)
        {
            // For new entities, stamp the tenant ID
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(ITenantScoped.TenantId)).CurrentValue = tenantId.Value;
            }
            // For modified entities, ensure the tenant ID hasn't been tampered with
            else if (entry.State == EntityState.Modified)
            {
                var originalTenantId = (Guid?)entry.Property(nameof(ITenantScoped.TenantId)).OriginalValue;
                
                // Reject cross-tenant modification attempts
                if (originalTenantId.HasValue && originalTenantId.Value != tenantId.Value)
                {
                    throw new InvalidOperationException(
                        $"Cannot modify entity '{entry.Entity.GetType().Name}' from tenant '{originalTenantId}' " +
                        $"in the context of tenant '{tenantId}'. Cross-tenant data mutation is not allowed.");
                }
            }
        }
    }
}