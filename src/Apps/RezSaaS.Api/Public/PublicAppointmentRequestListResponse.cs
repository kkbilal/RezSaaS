namespace RezSaaS.Api.PublicApi;

public sealed record PublicAppointmentRequestListResponse(
    IReadOnlyCollection<PublicAppointmentRequestResponse> Requests);
