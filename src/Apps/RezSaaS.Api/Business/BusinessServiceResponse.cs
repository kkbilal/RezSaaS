namespace RezSaaS.Api.Business;

public sealed record BusinessServiceResponse(
    Guid Id, string Name, string CategoryKey, string Status, DateTimeOffset CreatedAtUtc);
