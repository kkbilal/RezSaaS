namespace RezSaaS.Api.Admin;

public sealed record AdminAccountClosureProposalRequest(
    string InternalReason,
    string CustomerNotice);
