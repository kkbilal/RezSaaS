using Microsoft.AspNetCore.Identity;

namespace RezSaaS.Modules.Identity.Domain;

public sealed class UserAccount : IdentityUser<Guid>
{
    public AccountStatus Status { get; private set; } = AccountStatus.Active;

    public void Suspend()
    {
        Status = AccountStatus.Suspended;
    }

    public void Close()
    {
        Status = AccountStatus.Closed;
    }
}
