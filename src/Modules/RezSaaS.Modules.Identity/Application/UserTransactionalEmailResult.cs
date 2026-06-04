namespace RezSaaS.Modules.Identity.Application;

public sealed record UserTransactionalEmailResult(
    bool Succeeded,
    string? ErrorCode)
{
    public static UserTransactionalEmailResult Success()
    {
        return new UserTransactionalEmailResult(true, ErrorCode: null);
    }

    public static UserTransactionalEmailResult Failure(string errorCode)
    {
        return new UserTransactionalEmailResult(false, errorCode);
    }
}
