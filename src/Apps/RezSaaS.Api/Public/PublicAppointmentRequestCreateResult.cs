namespace RezSaaS.Api.PublicApi;

public sealed record PublicAppointmentRequestCreateResult(
    PublicAppointmentRequestCreateOutcome Outcome,
    PublicAppointmentRequestCreateResponse? Response,
    string? ErrorCode)
{
    public static PublicAppointmentRequestCreateResult Created(
        Guid appointmentRequestId,
        DateTimeOffset expiresAtUtc)
    {
        return new PublicAppointmentRequestCreateResult(
            PublicAppointmentRequestCreateOutcome.Created,
            new PublicAppointmentRequestCreateResponse(
                appointmentRequestId,
                expiresAtUtc,
                "PendingApproval"),
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
