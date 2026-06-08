namespace RezSaaS.Modules.Booking.Application;

public sealed record AppointmentOperationResult(
    bool Succeeded,
    Guid? AppointmentId,
    Guid? RelatedAppointmentId,
    string? Status,
    string? ErrorCode)
{
    public static AppointmentOperationResult Success(
        Guid appointmentId,
        string status,
        Guid? relatedAppointmentId = null)
    {
        return new AppointmentOperationResult(
            Succeeded: true,
            appointmentId,
            relatedAppointmentId,
            status,
            ErrorCode: null);
    }

    public static AppointmentOperationResult Failure(string errorCode)
    {
        return new AppointmentOperationResult(
            Succeeded: false,
            AppointmentId: null,
            RelatedAppointmentId: null,
            Status: null,
            errorCode);
    }
}
