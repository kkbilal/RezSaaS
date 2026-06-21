namespace RezSaaS.Modules.Resources.Application;

public sealed record ResourceTypeManagementResult(
    bool Succeeded,
    string? ErrorCode,
    ResourceTypeView? ResourceType,
    IReadOnlyCollection<ResourceTypeView>? ResourceTypes)
{
    public static ResourceTypeManagementResult Success(ResourceTypeView resourceType)
        => new(true, null, resourceType, null);
    public static ResourceTypeManagementResult SuccessList(IReadOnlyCollection<ResourceTypeView> resourceTypes)
        => new(true, null, null, resourceTypes);
    public static ResourceTypeManagementResult Failure(string errorCode)
        => new(false, errorCode, null, null);
}
