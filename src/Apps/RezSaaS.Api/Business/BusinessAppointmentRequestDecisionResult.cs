namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentRequestDecisionResult(
    BusinessAppointmentRequestOutcome Outcome,
    BusinessAppointmentRequestDecisionResponse? Response,
    string? ErrorCode)
{
    public static BusinessAppointmentRequestDecisionResult Success(
        BusinessAppointmentRequestDecisionResponse response)
    {
        return new BusinessAppointmentRequestDecisionResult(
            BusinessAppointmentRequestOutcome.Success,
            response,
            ErrorCode: null);
    }

    public static BusinessAppointmentRequestDecisionResult Failure(
        BusinessAppointmentRequestOutcome outcome,
        string errorCode)
    {
        return new BusinessAppointmentRequestDecisionResult(
            outcome,
            Response: null,
            errorCode);
    }
}
