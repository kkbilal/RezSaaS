namespace RezSaaS.Api.Business;

public sealed record BusinessResourceBlockResult(
    BusinessAppointmentOutcome Outcome,
    BusinessResourceBlockResponse? Response,
    string? ErrorCode)
{
    public static BusinessResourceBlockResult Success(BusinessResourceBlockResponse response)
    {
        return new BusinessResourceBlockResult(
            BusinessAppointmentOutcome.Success,
            response,
            ErrorCode: null);
    }

    public static BusinessResourceBlockResult Failure(
        BusinessAppointmentOutcome outcome,
        string errorCode)
    {
        return new BusinessResourceBlockResult(outcome, Response: null, errorCode);
    }
}
