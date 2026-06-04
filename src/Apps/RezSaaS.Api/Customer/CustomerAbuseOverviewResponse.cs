namespace RezSaaS.Api.Customer;

public sealed record CustomerAbuseOverviewResponse(
    IReadOnlyCollection<CustomerSanctionResponse> Sanctions,
    IReadOnlyCollection<CustomerStrikeResponse> Strikes,
    IReadOnlyCollection<CustomerAbuseAppealResponse> Appeals,
    IReadOnlyCollection<CustomerAccountClosureCaseResponse> ClosureCases);
