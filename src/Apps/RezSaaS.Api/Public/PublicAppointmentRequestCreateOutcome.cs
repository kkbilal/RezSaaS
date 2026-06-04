namespace RezSaaS.Api.PublicApi;

public enum PublicAppointmentRequestCreateOutcome
{
    Created,
    Replayed,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests,
    Unprocessable,
}
