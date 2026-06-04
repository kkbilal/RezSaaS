using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record ApplyUserSanctionCommand(
    Guid ActorUserAccountId,
    Guid UserAccountId,
    UserSanctionType Type,
    string Reason,
    DateTimeOffset? EndsAtUtc);
