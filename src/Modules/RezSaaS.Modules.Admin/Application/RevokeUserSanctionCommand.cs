namespace RezSaaS.Modules.Admin.Application;

public sealed record RevokeUserSanctionCommand(
    Guid ActorUserAccountId,
    Guid UserAccountId,
    Guid SanctionId,
    string Reason);
