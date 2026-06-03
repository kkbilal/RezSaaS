namespace RezSaaS.Api.PublicApi;

public sealed record PublicSlotSearchValidationResponse(
    IReadOnlyCollection<string> Errors);
