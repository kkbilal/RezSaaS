namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseAccessResult(
    AdminAbuseOutcome Outcome,
    IReadOnlyCollection<AdminAbuseEventResponse> Events,
    AdminUserAbuseOverviewResponse? Overview,
    AdminUserSanctionResponse? Sanction,
    string? ErrorCode)
{
    public static AdminAbuseAccessResult Success(
        IReadOnlyCollection<AdminAbuseEventResponse> events)
    {
        return new AdminAbuseAccessResult(
            AdminAbuseOutcome.Success,
            events,
            Overview: null,
            Sanction: null,
            ErrorCode: null);
    }

    public static AdminAbuseAccessResult Success(AdminUserAbuseOverviewResponse overview)
    {
        return new AdminAbuseAccessResult(
            AdminAbuseOutcome.Success,
            Events: [],
            overview,
            Sanction: null,
            ErrorCode: null);
    }

    public static AdminAbuseAccessResult Created(AdminUserSanctionResponse sanction)
    {
        return new AdminAbuseAccessResult(
            AdminAbuseOutcome.Created,
            Events: [],
            Overview: null,
            sanction,
            ErrorCode: null);
    }

    public static AdminAbuseAccessResult SuccessSanction(AdminUserSanctionResponse sanction)
    {
        return new AdminAbuseAccessResult(
            AdminAbuseOutcome.Success,
            Events: [],
            Overview: null,
            sanction,
            ErrorCode: null);
    }

    public static AdminAbuseAccessResult Failure(
        AdminAbuseOutcome outcome,
        string errorCode)
    {
        return new AdminAbuseAccessResult(
            outcome,
            Events: [],
            Overview: null,
            Sanction: null,
            errorCode);
    }
}
