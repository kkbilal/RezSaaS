namespace RezSaaS.Modules.Admin.Application;

public sealed record RevokeUserStrikeCommand(
    Guid ActorUserAccountId,
    Guid UserAccountId,
    Guid StrikeId,
    string Reason);
