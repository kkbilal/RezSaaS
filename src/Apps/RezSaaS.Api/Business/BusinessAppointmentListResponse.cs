namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentListResponse(
    IReadOnlyCollection<BusinessAppointmentResponse> Appointments);
