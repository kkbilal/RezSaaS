namespace RezSaaS.Api.PublicApi;

public enum PublicAppointmentRequestCreateOutcome
{
    Created,
    BadRequest,
    Unauthorized,
    NotFound,
    Conflict,
    TooManyRequests,
    Unprocessable,
}
