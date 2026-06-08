namespace RezSaaS.Api.Business;

public sealed record BusinessResourceBlockRequest(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string Reason);
