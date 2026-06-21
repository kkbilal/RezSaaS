namespace RezSaaS.Modules.Catalog.Application;

public sealed record ServiceVariantManagementResult(
    bool Succeeded,
    string? ErrorCode,
    ServiceVariantView? Variant,
    IReadOnlyCollection<ServiceVariantView>? Variants)
{
    public static ServiceVariantManagementResult Success(ServiceVariantView variant)
        => new(true, null, variant, null);

    public static ServiceVariantManagementResult SuccessList(IReadOnlyCollection<ServiceVariantView> variants)
        => new(true, null, null, variants);

    public static ServiceVariantManagementResult Failure(string errorCode)
        => new(false, errorCode, null, null);
}
