using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record CreateAbuseAppealCommand(
    Guid UserAccountId,
    AbuseAppealTargetType TargetType,
    Guid TargetId,
    string Statement);
