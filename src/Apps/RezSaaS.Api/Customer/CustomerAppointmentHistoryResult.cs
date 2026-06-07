namespace RezSaaS.Api.Customer;

public sealed record CustomerAppointmentHistoryResult(
    CustomerAppointmentHistoryOutcome Outcome,
    IReadOnlyCollection<CustomerAppointmentHistoryItemResponse> Items,
    string? ErrorCode)
{
    public static CustomerAppointmentHistoryResult Success(
        IReadOnlyCollection<CustomerAppointmentHistoryItemResponse> items)
    {
        return new CustomerAppointmentHistoryResult(
            CustomerAppointmentHistoryOutcome.Success,
            items,
            null);
    }

    public static CustomerAppointmentHistoryResult Failure(
        CustomerAppointmentHistoryOutcome outcome,
        string errorCode)
    {
        return new CustomerAppointmentHistoryResult(outcome, [], errorCode);
    }
}
