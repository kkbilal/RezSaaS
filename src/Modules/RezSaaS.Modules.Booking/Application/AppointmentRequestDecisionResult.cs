namespace RezSaaS.Modules.Booking.Application;

public sealed record AppointmentRequestDecisionResult(
    bool Succeeded,
    Guid? AppointmentId,
    int AffectedRequests,
    string? ErrorCode)
{
    public static AppointmentRequestDecisionResult Success(
        Guid? appointmentId,
        int affectedRequests = 0)
    {
        return new AppointmentRequestDecisionResult(
            true,
            appointmentId,
            affectedRequests,
            ErrorCode: null);
    }

    public static AppointmentRequestDecisionResult Failure(string errorCode)
    {
        return new AppointmentRequestDecisionResult(
            false,
            AppointmentId: null,
            AffectedRequests: 0,
            errorCode);
    }
}
