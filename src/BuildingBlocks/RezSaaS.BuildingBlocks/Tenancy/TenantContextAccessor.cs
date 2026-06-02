namespace RezSaaS.BuildingBlocks.Tenancy;

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    public Guid? TenantId { get; set; }
}
