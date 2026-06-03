namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentRequestListResult(
    BusinessAppointmentRequestOutcome Outcome,
    IReadOnlyCollection<BusinessAppointmentRequestResponse> Requests,
    string? ErrorCode)
{
    public static BusinessAppointmentRequestListResult Success(
        IReadOnlyCollection<BusinessAppointmentRequestResponse> requests)
    {
        return new BusinessAppointmentRequestListResult(
            BusinessAppointmentRequestOutcome.Success,
            requests,
            ErrorCode: null);
    }

    public static BusinessAppointmentRequestListResult Failure(
        BusinessAppointmentRequestOutcome outcome,
        string errorCode)
    {
        return new BusinessAppointmentRequestListResult(
            outcome,
            [],
            errorCode);
    }
}
