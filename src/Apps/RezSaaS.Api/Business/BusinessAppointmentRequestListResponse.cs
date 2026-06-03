namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentRequestListResponse(
    IReadOnlyCollection<BusinessAppointmentRequestResponse> Requests);
