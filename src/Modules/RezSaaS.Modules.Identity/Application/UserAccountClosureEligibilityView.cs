using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Modules.Identity.Application;

public sealed record UserAccountClosureEligibilityView(
    Guid UserAccountId,
    AccountStatus Status,
    bool HasPlatformRole)
{
    public bool CanExecuteClosure =>
        !HasPlatformRole && Status is AccountStatus.Active or AccountStatus.Closed;

    public bool CanProposeClosure =>
        !HasPlatformRole && Status == AccountStatus.Active;
}
