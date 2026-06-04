namespace RezSaaS.Api.Customer;

public sealed record CustomerAbuseAccessResult(
    CustomerAbuseOutcome Outcome,
    CustomerAbuseOverviewResponse? Overview,
    CustomerAbuseAppealResponse? Appeal,
    string? ErrorCode)
{
    public static CustomerAbuseAccessResult Success(CustomerAbuseOverviewResponse overview)
    {
        return new CustomerAbuseAccessResult(
            CustomerAbuseOutcome.Success,
            overview,
            Appeal: null,
            ErrorCode: null);
    }

    public static CustomerAbuseAccessResult Created(CustomerAbuseAppealResponse appeal)
    {
        return new CustomerAbuseAccessResult(
            CustomerAbuseOutcome.Created,
            Overview: null,
            appeal,
            ErrorCode: null);
    }

    public static CustomerAbuseAccessResult Success(CustomerAbuseAppealResponse appeal)
    {
        return new CustomerAbuseAccessResult(
            CustomerAbuseOutcome.Success,
            Overview: null,
            appeal,
            ErrorCode: null);
    }

    public static CustomerAbuseAccessResult Failure(
        CustomerAbuseOutcome outcome,
        string errorCode)
    {
        return new CustomerAbuseAccessResult(
            outcome,
            Overview: null,
            Appeal: null,
            errorCode);
    }
}
