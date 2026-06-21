namespace RezSaaS.Api.Business;

public sealed record BusinessServiceResult(
    BusinessServiceOutcome Outcome,
    string? ErrorCode,
    BusinessServiceResponse? Service,
    IReadOnlyCollection<BusinessServiceResponse>? Services)
{
    public static BusinessServiceResult Success(BusinessServiceResponse service)
        => new(BusinessServiceOutcome.Success, null, service, null);
    public static BusinessServiceResult SuccessList(IReadOnlyCollection<BusinessServiceResponse> services)
        => new(BusinessServiceOutcome.Success, null, null, services);
    public static BusinessServiceResult Failure(BusinessServiceOutcome outcome, string errorCode)
        => new(outcome, errorCode, null, null);
}
