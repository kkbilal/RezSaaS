using RezSaaS.Modules.Booking.Domain;

namespace RezSaaS.Modules.Booking.Application;

public static class AppointmentRequestStatusFilter
{
    public static bool TryParse(
        string? status,
        out AppointmentRequestStatus parsedStatus)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = default;
            return false;
        }

        return Enum.TryParse(status, ignoreCase: true, out parsedStatus)
            && Enum.IsDefined(parsedStatus);
    }

    public static bool IsValidOrEmpty(string? status)
    {
        return string.IsNullOrWhiteSpace(status)
            || TryParse(status, out _);
    }
}
