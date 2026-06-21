namespace RezSaaS.Modules.Analytics.Domain.ReadModels;

public sealed class TopServiceMetrics
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? BranchId { get; init; }
    
    public Guid ServiceId { get; init; }
    public Guid? VariantId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? VariantName { get; init; }
    
    // Booking counts
    public int TotalBookings { get; init; }
    public int CompletedBookings { get; init; }
    public int CancelledBookings { get; init; }
    public int NoShowBookings { get; init; }
    
    // Revenue (if payments are enabled)
    public decimal TotalRevenue { get; init; }
    
    // Duration stats
    public decimal AverageServiceDurationMinutes { get; init; }
    
    // Ranking (for top lists)
    public int Ranking { get; init; }
    
    // Time period
    public DateTime PeriodStartUtc { get; init; }
    public DateTime PeriodEndUtc { get; init; }
    
    public DateTimeOffset GeneratedAtUtc { get; init; }
}