namespace RezSaaS.Modules.Analytics.Domain.ReadModels;

public sealed class ResourceCapacityMetrics
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? BranchId { get; init; }
    
    public Guid ResourceId { get; init; }
    public string ResourceName { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    
    // Capacity metrics
    public int TotalAvailableSlots { get; init; }
    public int BookedSlots { get; init; }
    public int AvailableSlots { get; init; }
    public decimal CapacityUtilizationRate { get; init; }
    
    // Time-based utilization
    public decimal TotalBookedMinutes { get; init; }
    public decimal TotalAvailableMinutes { get; init; }
    public decimal TimeUtilizationRate { get; init; }
    
    // Staff assignment (if applicable)
    public Guid? AssignedStaffId { get; init; }
    public string? StaffName { get; init; }
    
    // Time period
    public DateTime PeriodStartUtc { get; init; }
    public DateTime PeriodEndUtc { get; init; }
    
    public DateTimeOffset GeneratedAtUtc { get; init; }
}