namespace RezSaaS.Api.Business;

public sealed record BusinessStaffUnavailableCreateRequest(
    DateTimeOffset StartUtc, DateTimeOffset EndUtc, string Reason);
