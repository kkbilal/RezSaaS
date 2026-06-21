namespace RezSaaS.Modules.Analytics.Domain.ReadModels;

public sealed class DailyBusinessMetrics
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? BranchId { get; init; }
    public DateTime DateUtc { get; init; }
    
    // Request metrics
    public int TotalRequests { get; init; }
    public int ApprovedRequests { get; init; }
    public int DeclinedRequests { get; init; }
    public int ExpiredRequests { get; init; }
    
    // Appointment metrics
    public int TotalAppointments { get; init; }
    public int CompletedAppointments { get; init; }
    public int CancelledAppointments { get; init; }
    public int NoShowAppointments { get; init; }
    
    // Capacity metrics
    public int TotalSlots { get; init; }
    public int BookedSlots { get; init; }
    public decimal OccupancyRate { get; init; }
    
    // Time-based metrics (in minutes)
    public decimal TotalServiceDurationMinutes { get; init; }
    public decimal UtilizationRate { get; init; }
    
    // Conversion metrics
    public decimal RequestToApprovalRate { get; init; }
    
    // No-show metrics
    public decimal NoShowRate { get; init; }
    
    public DateTimeOffset GeneratedAtUtc { get; init; }
}