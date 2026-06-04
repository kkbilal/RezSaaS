namespace RezSaaS.Modules.Admin.Application;

public sealed record ProposeAccountClosureCommand(
    Guid ActorUserAccountId,
    Guid UserAccountId,
    string InternalReason,
    string CustomerNotice);
