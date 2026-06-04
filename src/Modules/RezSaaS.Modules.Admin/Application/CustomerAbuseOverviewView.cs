namespace RezSaaS.Modules.Admin.Application;

public sealed record CustomerAbuseOverviewView(
    Guid UserAccountId,
    IReadOnlyCollection<CustomerUserSanctionView> Sanctions,
    IReadOnlyCollection<CustomerUserStrikeView> Strikes,
    IReadOnlyCollection<CustomerAbuseAppealView> Appeals,
    IReadOnlyCollection<CustomerAccountClosureCaseView> ClosureCases);
