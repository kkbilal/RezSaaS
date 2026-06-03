namespace RezSaaS.Modules.Booking.Application;

public sealed record CreateAppointmentRequestResult(
    bool Succeeded,
    Guid? AppointmentRequestId,
    DateTimeOffset? ExpiresAtUtc,
    string? Status,
    bool IsReplay,
    string? ErrorCode)
{
    public static CreateAppointmentRequestResult Success(
        Guid appointmentRequestId,
        DateTimeOffset expiresAtUtc,
        string status = "PendingApproval",
        bool isReplay = false)
    {
        return new CreateAppointmentRequestResult(
            true,
            appointmentRequestId,
            expiresAtUtc,
            status,
            isReplay,
            ErrorCode: null);
    }

    public static CreateAppointmentRequestResult Failure(string errorCode)
    {
        return new CreateAppointmentRequestResult(
            false,
            AppointmentRequestId: null,
            ExpiresAtUtc: null,
            Status: null,
            IsReplay: false,
            errorCode);
    }
}
