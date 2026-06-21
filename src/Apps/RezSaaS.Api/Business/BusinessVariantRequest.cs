namespace RezSaaS.Api.Business;

public sealed record BusinessVariantCreateRequest(
    string Name, int DurationMinutes, decimal PriceAmount, string CurrencyCode, Guid? RequiredResourceTypeId);

public sealed record BusinessVariantUpdateRequest(
    string Name, int DurationMinutes, decimal PriceAmount, string CurrencyCode, Guid? RequiredResourceTypeId);
