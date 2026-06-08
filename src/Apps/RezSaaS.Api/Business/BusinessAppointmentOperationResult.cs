namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentOperationResult(
    BusinessAppointmentOutcome Outcome,
    BusinessAppointmentOperationResponse? Response,
    string? ErrorCode)
{
    public static BusinessAppointmentOperationResult Success(
        BusinessAppointmentOperationResponse response)
    {
        return new BusinessAppointmentOperationResult(
            BusinessAppointmentOutcome.Success,
            response,
            ErrorCode: null);
    }

    public static BusinessAppointmentOperationResult Failure(
        BusinessAppointmentOutcome outcome,
        string errorCode)
    {
        return new BusinessAppointmentOperationResult(outcome, Response: null, errorCode);
    }
}
