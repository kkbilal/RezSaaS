namespace RezSaaS.Api.Business;

public sealed record BusinessWorkingHoursUpsertRequest(
    string OpensAt, string ClosesAt, bool IsClosed);
