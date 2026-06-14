namespace RezSaaS.Modules.Integrations.Application;

public sealed record IntegrationLifecycleResult(
    bool Succeeded,
    string? ErrorCode,
    Guid? ResourceId)
{
    public static IntegrationLifecycleResult Success(Guid resourceId)
    {
        return new IntegrationLifecycleResult(true, null, resourceId);
    }

    public static IntegrationLifecycleResult Failure(string errorCode)
    {
        return new IntegrationLifecycleResult(false, errorCode, null);
    }
}
