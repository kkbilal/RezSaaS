namespace RezSaaS.Api.PublicApi;

public sealed record PublicAppointmentRequestCreateResult(
    PublicAppointmentRequestCreateOutcome Outcome,
    PublicAppointmentRequestCreateResponse? Response,
    string? ErrorCode)
{
    public static PublicAppointmentRequestCreateResult Created(
        Guid appointmentRequestId,
        DateTimeOffset expiresAtUtc,
        string status)
    {
        return new PublicAppointmentRequestCreateResult(
            PublicAppointmentRequestCreateOutcome.Created,
            new PublicAppointmentRequestCreateResponse(
                appointmentRequestId,
                expiresAtUtc,
                status),
            ErrorCode: null);
    }

    public static PublicAppointmentRequestCreateResult Replayed(
        Guid appointmentRequestId,
        DateTimeOffset expiresAtUtc,
        string status)
    {
        return new PublicAppointmentRequestCreateResult(
            PublicAppointmentRequestCreateOutcome.Replayed,
            new PublicAppointmentRequestCreateResponse(
                appointmentRequestId,
                expiresAtUtc,
                status),
            ErrorCode: null);
    }

    public static PublicAppointmentRequestCreateResult Failure(
        PublicAppointmentRequestCreateOutcome outcome,
        string errorCode)
    {
        return new PublicAppointmentRequestCreateResult(
            outcome,
            Response: null,
            errorCode);
    }
}
