namespace RezSaaS.Api.Business;

public sealed record BusinessResourceResult(
    BusinessResourceOutcome Outcome,
    string? ErrorCode,
    BusinessResourceResponse? Resource,
    IReadOnlyCollection<BusinessResourceResponse>? Resources)
{
    public static BusinessResourceResult Success(BusinessResourceResponse resource)
        => new(BusinessResourceOutcome.Success, null, resource, null);
    public static BusinessResourceResult SuccessList(IReadOnlyCollection<BusinessResourceResponse> resources)
        => new(BusinessResourceOutcome.Success, null, null, resources);
    public static BusinessResourceResult Failure(BusinessResourceOutcome outcome, string errorCode)
        => new(outcome, errorCode, null, null);
}
