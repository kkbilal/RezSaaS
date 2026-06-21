namespace RezSaaS.Api.Business;

public sealed record BusinessBranchResult(
    BusinessBranchOutcome Outcome,
    string? ErrorCode,
    BusinessBranchResponse? Branch,
    IReadOnlyCollection<BusinessBranchResponse>? Branches)
{
    public static BusinessBranchResult Success(BusinessBranchResponse branch)
    {
        return new BusinessBranchResult(BusinessBranchOutcome.Success, null, branch, null);
    }

    public static BusinessBranchResult SuccessList(IReadOnlyCollection<BusinessBranchResponse> branches)
    {
        return new BusinessBranchResult(BusinessBranchOutcome.Success, null, null, branches);
    }

    public static BusinessBranchResult Failure(
        BusinessBranchOutcome outcome,
        string errorCode)
    {
        return new BusinessBranchResult(outcome, errorCode, null, null);
    }
}
