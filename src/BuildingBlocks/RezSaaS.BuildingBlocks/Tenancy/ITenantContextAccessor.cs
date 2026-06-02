namespace RezSaaS.BuildingBlocks.Tenancy;

public interface ITenantContextAccessor
{
    Guid? TenantId { get; set; }
}
