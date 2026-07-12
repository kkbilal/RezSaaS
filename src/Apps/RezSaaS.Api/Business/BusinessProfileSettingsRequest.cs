namespace RezSaaS.Api.Business;

public sealed record BusinessProfileSettingsRequest(
    string? DisplayName,
    string? Description,
    string? PublicRules,
    string? SeoTitle,
    string? SeoDescription,
    string? StaffDisplayPolicy,
    // Gonderilmezse mevcut iptal politikasi KORUNUR (sifirlanmaz).
    int? CancellationCutoffHours);
