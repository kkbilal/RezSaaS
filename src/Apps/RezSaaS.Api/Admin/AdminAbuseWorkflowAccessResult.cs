namespace RezSaaS.Api.Admin;

public sealed record AdminAbuseWorkflowAccessResult(
    AdminAbuseOutcome Outcome,
    IReadOnlyCollection<AdminAbuseAppealResponse> Appeals,
    IReadOnlyCollection<AdminAccountClosureCaseResponse> ClosureCases,
    AdminAbuseAppealResponse? Appeal,
    AdminAccountClosureCaseResponse? ClosureCase,
    string? ErrorCode)
{
    public static AdminAbuseWorkflowAccessResult Success(
        IReadOnlyCollection<AdminAbuseAppealResponse> appeals)
    {
        return new AdminAbuseWorkflowAccessResult(
            AdminAbuseOutcome.Success,
            appeals,
            ClosureCases: [],
            Appeal: null,
            ClosureCase: null,
            ErrorCode: null);
    }

    public static AdminAbuseWorkflowAccessResult Success(
        IReadOnlyCollection<AdminAccountClosureCaseResponse> closureCases)
    {
        return new AdminAbuseWorkflowAccessResult(
            AdminAbuseOutcome.Success,
            Appeals: [],
            closureCases,
            Appeal: null,
            ClosureCase: null,
            ErrorCode: null);
    }

    public static AdminAbuseWorkflowAccessResult Success(AdminAbuseAppealResponse appeal)
    {
        return new AdminAbuseWorkflowAccessResult(
            AdminAbuseOutcome.Success,
            Appeals: [],
            ClosureCases: [],
            appeal,
            ClosureCase: null,
            ErrorCode: null);
    }

    public static AdminAbuseWorkflowAccessResult Success(AdminAccountClosureCaseResponse closureCase)
    {
        return new AdminAbuseWorkflowAccessResult(
            AdminAbuseOutcome.Success,
            Appeals: [],
            ClosureCases: [],
            Appeal: null,
            closureCase,
            ErrorCode: null);
    }

    public static AdminAbuseWorkflowAccessResult Created(AdminAccountClosureCaseResponse closureCase)
    {
        return new AdminAbuseWorkflowAccessResult(
            AdminAbuseOutcome.Created,
            Appeals: [],
            ClosureCases: [],
            Appeal: null,
            closureCase,
            ErrorCode: null);
    }

    public static AdminAbuseWorkflowAccessResult Failure(
        AdminAbuseOutcome outcome,
        string errorCode)
    {
        return new AdminAbuseWorkflowAccessResult(
            outcome,
            Appeals: [],
            ClosureCases: [],
            Appeal: null,
            ClosureCase: null,
            errorCode);
    }
}
