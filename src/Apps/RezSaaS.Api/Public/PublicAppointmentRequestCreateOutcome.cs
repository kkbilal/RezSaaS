namespace RezSaaS.Api.PublicApi;

public enum PublicAppointmentRequestCreateOutcome
{
    Created,
    Replayed,
    BadRequest,
    Unauthorized,
    NotFound,
    Conflict,
    TooManyRequests,
    Unprocessable,
}
