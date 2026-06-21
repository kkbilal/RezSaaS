namespace RezSaaS.BuildingBlocks.Tenancy;

/// <summary>
/// Tenant-scoped entity'ler için marker interface.
/// Her tenant-scoped entity bu interface'i implement etmeli.
/// </summary>
public interface ITenantScoped
{
    /// <summary>
    /// Entity'nin ait olduğu tenant ID'si.
    /// </summary>
    Guid TenantId { get; }
}