namespace RezSaaS.Api.Business;

public sealed record BusinessResourceTypeResult(
    BusinessResourceTypeOutcome Outcome,
    string? ErrorCode,
    BusinessResourceTypeResponse? ResourceType,
    IReadOnlyCollection<BusinessResourceTypeResponse>? ResourceTypes)
{
    public static BusinessResourceTypeResult Success(BusinessResourceTypeResponse resourceType)
        => new(BusinessResourceTypeOutcome.Success, null, resourceType, null);
    public static BusinessResourceTypeResult SuccessList(IReadOnlyCollection<BusinessResourceTypeResponse> resourceTypes)
        => new(BusinessResourceTypeOutcome.Success, null, null, resourceTypes);
    public static BusinessResourceTypeResult Failure(BusinessResourceTypeOutcome outcome, string errorCode)
        => new(outcome, errorCode, null, null);
}
