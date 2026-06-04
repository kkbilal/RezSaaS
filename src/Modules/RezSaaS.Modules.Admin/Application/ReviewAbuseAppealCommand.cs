using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record ReviewAbuseAppealCommand(
    Guid ActorUserAccountId,
    Guid AppealId,
    AbuseAppealStatus Decision,
    string Reason);
