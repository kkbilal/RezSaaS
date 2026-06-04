namespace RezSaaS.Modules.Identity.Application;

public sealed record UserAccountClosureResult(
    bool Succeeded,
    bool AlreadyClosed,
    string? ErrorCode)
{
    public static UserAccountClosureResult Success(bool alreadyClosed = false)
    {
        return new UserAccountClosureResult(
            true,
            alreadyClosed,
            ErrorCode: null);
    }

    public static UserAccountClosureResult Failure(string errorCode)
    {
        return new UserAccountClosureResult(
            false,
            AlreadyClosed: false,
            errorCode);
    }
}
