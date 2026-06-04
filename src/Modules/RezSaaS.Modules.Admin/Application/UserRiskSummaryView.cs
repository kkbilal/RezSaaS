using RezSaaS.Modules.Admin.Domain;

namespace RezSaaS.Modules.Admin.Application;

public sealed record UserRiskSummaryView(
    int ActiveStrikeCount,
    UserRiskLevel Level);
