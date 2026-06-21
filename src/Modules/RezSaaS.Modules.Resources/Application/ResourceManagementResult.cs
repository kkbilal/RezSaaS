namespace RezSaaS.Modules.Resources.Application;

public sealed record ResourceManagementResult(
    bool Succeeded,
    string? ErrorCode,
    ResourceView? Resource,
    IReadOnlyCollection<ResourceView>? Resources)
{
    public static ResourceManagementResult Success(ResourceView resource)
        => new(true, null, resource, null);
    public static ResourceManagementResult SuccessList(IReadOnlyCollection<ResourceView> resources)
        => new(true, null, null, resources);
    public static ResourceManagementResult Failure(string errorCode)
        => new(false, errorCode, null, null);
}
