namespace RezSaaS.Modules.Admin.Application;

public sealed record UserStrikeCommandResult(
    bool Succeeded,
    Guid? StrikeId,
    string? ErrorCode)
{
    public static UserStrikeCommandResult Success(Guid strikeId)
    {
        return new UserStrikeCommandResult(
            true,
            strikeId,
            ErrorCode: null);
    }

    public static UserStrikeCommandResult Failure(string errorCode)
    {
        return new UserStrikeCommandResult(
            false,
            StrikeId: null,
            errorCode);
    }
}
