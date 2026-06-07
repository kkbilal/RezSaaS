namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed record StepUpSessionResult(
    bool Succeeded,
    string? ErrorCode,
    string? Token,
    StepUpSessionView? Session)
{
    public static StepUpSessionResult Success(
        string token,
        StepUpSessionView session)
    {
        return new StepUpSessionResult(true, null, token, session);
    }

    public static StepUpSessionResult Failure(string errorCode)
    {
        return new StepUpSessionResult(false, errorCode, null, null);
    }
}
