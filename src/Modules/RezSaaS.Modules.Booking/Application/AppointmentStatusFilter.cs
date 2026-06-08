using RezSaaS.Modules.Booking.Domain;

namespace RezSaaS.Modules.Booking.Application;

public static class AppointmentStatusFilter
{
    public static bool IsValidOrEmpty(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            || Enum.TryParse(status, ignoreCase: true, out AppointmentStatus _);
    }

    public static bool TryParse(string? status, out AppointmentStatus parsedStatus)
    {
        parsedStatus = default;

        return !string.IsNullOrWhiteSpace(status)
            && Enum.TryParse(status, ignoreCase: true, out parsedStatus);
    }
}
