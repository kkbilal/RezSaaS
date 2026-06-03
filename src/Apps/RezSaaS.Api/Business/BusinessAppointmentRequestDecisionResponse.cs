namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentRequestDecisionResponse(
    Guid? AppointmentId,
    int AffectedRequests,
    string Status);
