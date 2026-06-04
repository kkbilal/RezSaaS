using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record ReviewAccountClosureCommand(
    Guid ActorUserAccountId,
    Guid ClosureCaseId,
    AccountClosureCaseStatus Decision,
    string Reason);
