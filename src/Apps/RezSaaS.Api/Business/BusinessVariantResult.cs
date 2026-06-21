namespace RezSaaS.Api.Business;

public sealed record BusinessVariantResult(
    BusinessVariantOutcome Outcome,
    string? ErrorCode,
    BusinessVariantResponse? Variant,
    IReadOnlyCollection<BusinessVariantResponse>? Variants)
{
    public static BusinessVariantResult Success(BusinessVariantResponse variant)
        => new(BusinessVariantOutcome.Success, null, variant, null);
    public static BusinessVariantResult SuccessList(IReadOnlyCollection<BusinessVariantResponse> variants)
        => new(BusinessVariantOutcome.Success, null, null, variants);
    public static BusinessVariantResult Failure(BusinessVariantOutcome outcome, string errorCode)
        => new(outcome, errorCode, null, null);
}
