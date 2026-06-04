namespace RezSaaS.Modules.Admin.Application;

public sealed record ApplyUserSanctionResult(
    bool Succeeded,
    Guid? SanctionId,
    string? ErrorCode)
{
    public static ApplyUserSanctionResult Success(Guid sanctionId)
    {
        return new ApplyUserSanctionResult(
            true,
            sanctionId,
            ErrorCode: null);
    }

    public static ApplyUserSanctionResult Failure(string errorCode)
    {
        return new ApplyUserSanctionResult(
            false,
            SanctionId: null,
            errorCode);
    }
}
