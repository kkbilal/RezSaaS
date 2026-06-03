namespace RezSaaS.Api.PublicApi;

public sealed record PublicAppointmentRequestAccessResult(
    PublicAppointmentRequestAccessOutcome Outcome,
    IReadOnlyCollection<PublicAppointmentRequestResponse> Requests,
    PublicAppointmentRequestResponse? Request,
    string? ErrorCode)
{
    public static PublicAppointmentRequestAccessResult Success(
        IReadOnlyCollection<PublicAppointmentRequestResponse> requests)
    {
        return new PublicAppointmentRequestAccessResult(
            PublicAppointmentRequestAccessOutcome.Success,
            requests,
            Request: null,
            ErrorCode: null);
    }

    public static PublicAppointmentRequestAccessResult Success(
        PublicAppointmentRequestResponse request)
    {
        return new PublicAppointmentRequestAccessResult(
            PublicAppointmentRequestAccessOutcome.Success,
            Requests: [],
            request,
            ErrorCode: null);
    }

    public static PublicAppointmentRequestAccessResult Failure(
        PublicAppointmentRequestAccessOutcome outcome,
        string errorCode)
    {
        return new PublicAppointmentRequestAccessResult(
            outcome,
            Requests: [],
            Request: null,
            errorCode);
    }
}
