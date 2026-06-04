namespace RezSaaS.Modules.Admin.Application;

public sealed record ExecuteAccountClosureCommand(
    Guid ActorUserAccountId,
    Guid ClosureCaseId);
