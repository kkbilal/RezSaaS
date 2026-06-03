namespace RezSaaS.Api.PublicApi;

public enum PublicAppointmentRequestAccessOutcome
{
    Success,
    BadRequest,
    Unauthorized,
    NotFound,
    Conflict,
    Unprocessable,
}
