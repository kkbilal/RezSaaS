namespace RezSaaS.Modules.Booking.Application;

public sealed record CreateAppointmentRequestResult(
    bool Succeeded,
    Guid? AppointmentRequestId,
    DateTimeOffset? ExpiresAtUtc,
    string? ErrorCode)
{
    public static CreateAppointmentRequestResult Success(
        Guid appointmentRequestId,
        DateTimeOffset expiresAtUtc)
    {
        return new CreateAppointmentRequestResult(
            true,
            appointmentRequestId,
            expiresAtUtc,
            ErrorCode: null);
    }

    public static CreateAppointmentRequestResult Failure(string errorCode)
    {
        return new CreateAppointmentRequestResult(
            false,
            AppointmentRequestId: null,
            ExpiresAtUtc: null,
            errorCode);
    }
}
