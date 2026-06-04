namespace RezSaaS.Api.Customer;

public sealed record CustomerCreateAbuseAppealRequest(
    string TargetType,
    Guid TargetId,
    string Statement);
