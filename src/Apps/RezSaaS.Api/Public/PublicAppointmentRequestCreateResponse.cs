namespace RezSaaS.Api.PublicApi;

public sealed record PublicAppointmentRequestCreateResponse(
    Guid AppointmentRequestId,
    DateTimeOffset ExpiresAtUtc,
    string Status);
