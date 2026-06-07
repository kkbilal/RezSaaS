namespace RezSaaS.Api.Customer;

public sealed record CustomerAppointmentHistoryResponse(
    IReadOnlyCollection<CustomerAppointmentHistoryItemResponse> Items);
