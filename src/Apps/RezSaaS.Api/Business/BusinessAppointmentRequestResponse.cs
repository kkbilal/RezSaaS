namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentRequestResponse(
    Guid Id,
    Guid CustomerUserAccountId,
    BusinessAppointmentRequestCustomerResponse Customer,
    Guid BranchId,
    string BranchDisplayName,
    string BranchTimeZoneId,
    Guid StaffMemberId,
    string StaffMemberDisplayName,
    Guid ResourceId,
    string ResourceDisplayName,
    DateTimeOffset RequestedStartUtc,
    DateTimeOffset RequestedEndUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    IReadOnlyCollection<BusinessAppointmentRequestLineResponse> Lines);
