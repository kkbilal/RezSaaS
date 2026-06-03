namespace RezSaaS.Modules.Booking.Application;

public sealed class BookingSecurityOptions
{
    public const string SectionName = "Booking:Security";

    public int AppointmentRequestPermitLimit { get; init; } = 12;

    public int AppointmentRequestWindowMinutes { get; init; } = 1;

    public int BusinessDecisionPermitLimit { get; init; } = 60;

    public int BusinessDecisionWindowMinutes { get; init; } = 1;

    public TimeSpan DefaultResponseBuffer { get; init; } = TimeSpan.FromHours(2);

    public int MaxConcurrentPendingRequestsPerUser { get; init; } = 3;

    public int MaxRequestsPerUserPerDay { get; init; } = 20;
}
