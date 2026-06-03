namespace RezSaaS.Api.Business;

public sealed record BusinessAppointmentRequestCustomerResponse(
    Guid UserAccountId,
    string MaskedEmail,
    string MaskedPhone);
