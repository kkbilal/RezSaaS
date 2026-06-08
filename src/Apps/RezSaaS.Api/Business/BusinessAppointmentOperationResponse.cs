namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentOperationResponse(
    Guid AppointmentId,
    Guid? RelatedAppointmentId,
    string Status);
