namespace RezSaaS.Modules.Admin.Application;

public sealed record UserAbuseOverviewView(
    Guid UserAccountId,
    IReadOnlyCollection<AbuseEventView> Events,
    IReadOnlyCollection<UserSanctionView> Sanctions);
