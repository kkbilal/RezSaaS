namespace RezSaaS.BuildingBlocks.Tenancy;

/// <summary>
/// Provides access to the current tenant identifier in the request context.
/// </summary>
public interface ITenantAccessor
{
    /// <summary>
    /// Gets the current tenant ID.
    /// </summary>
    Guid? TenantId { get; }
}