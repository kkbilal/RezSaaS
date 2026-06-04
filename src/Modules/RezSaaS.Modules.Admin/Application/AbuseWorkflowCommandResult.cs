namespace RezSaaS.Modules.Admin.Application;

public sealed record AbuseWorkflowCommandResult(
    bool Succeeded,
    Guid? EntityId,
    bool Created,
    string? ErrorCode)
{
    public static AbuseWorkflowCommandResult Success(Guid entityId, bool created = false)
    {
        return new AbuseWorkflowCommandResult(
            true,
            entityId,
            created,
            ErrorCode: null);
    }

    public static AbuseWorkflowCommandResult Failure(string errorCode)
    {
        return new AbuseWorkflowCommandResult(
            false,
            EntityId: null,
            Created: false,
            errorCode);
    }
}
