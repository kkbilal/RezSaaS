namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentListResult(
    BusinessAppointmentOutcome Outcome,
    IReadOnlyCollection<BusinessAppointmentResponse> Appointments,
    string? ErrorCode)
{
    public static BusinessAppointmentListResult Success(
        IReadOnlyCollection<BusinessAppointmentResponse> appointments)
    {
        return new BusinessAppointmentListResult(
            BusinessAppointmentOutcome.Success,
            appointments,
            ErrorCode: null);
    }

    public static BusinessAppointmentListResult Failure(
        BusinessAppointmentOutcome outcome,
        string errorCode)
    {
        return new BusinessAppointmentListResult(outcome, [], errorCode);
    }
}
