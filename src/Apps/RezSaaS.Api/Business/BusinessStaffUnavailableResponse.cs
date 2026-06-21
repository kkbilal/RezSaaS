namespace RezSaaS.Api.Business;

public sealed record BusinessStaffUnavailableResponse(
    Guid Id, Guid StaffMemberId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string Reason);
