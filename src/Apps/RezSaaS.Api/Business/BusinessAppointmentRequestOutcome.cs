namespace RezSaaS.Api.Business;

public enum BusinessAppointmentRequestOutcome
{
    Success,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Unprocessable,
}
