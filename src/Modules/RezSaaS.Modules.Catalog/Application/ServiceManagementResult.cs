namespace RezSaaS.Modules.Catalog.Application;

public sealed record ServiceManagementResult(
    bool Succeeded,
    string? ErrorCode,
    ServiceView? Service,
    IReadOnlyCollection<ServiceView>? Services)
{
    public static ServiceManagementResult Success(ServiceView service)
        => new(true, null, service, null);

    public static ServiceManagementResult SuccessList(IReadOnlyCollection<ServiceView> services)
        => new(true, null, null, services);

    public static ServiceManagementResult Failure(string errorCode)
        => new(false, errorCode, null, null);
}
