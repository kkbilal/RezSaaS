namespace RezSaaS.Api.Business;

public enum BusinessAbuseReportOutcome
{
    Success,
    Created,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    TooManyRequests,
    Unprocessable,
}
