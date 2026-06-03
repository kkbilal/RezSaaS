namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentRequestResponse(
    Guid Id,
    Guid CustomerUserAccountId,
    BusinessAppointmentRequestCustomerResponse Customer,
    Guid BranchId,
    Guid StaffMemberId,
    Guid ResourceId,
    DateTimeOffset RequestedStartUtc,
    DateTimeOffset RequestedEndUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status,
    IReadOnlyCollection<BusinessAppointmentRequestLineResponse> Lines);
