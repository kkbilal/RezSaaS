namespace RezSaaS.Modules.Organization.Application;

public sealed record BranchManagementResult(
    bool Succeeded,
    string? ErrorCode,
    BranchView? Branch,
    IReadOnlyCollection<BranchView>? Branches)
{
    public static BranchManagementResult Success(BranchView branch)
    {
        return new BranchManagementResult(true, null, branch, null);
    }

    public static BranchManagementResult SuccessList(IReadOnlyCollection<BranchView> branches)
    {
        return new BranchManagementResult(true, null, null, branches);
    }

    public static BranchManagementResult Failure(string errorCode)
    {
        return new BranchManagementResult(false, errorCode, null, null);
    }
}
