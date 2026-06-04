using Microsoft.AspNetCore.Identity;

namespace RezSaaS.Modules.Identity.Domain;

public sealed class UserAccount : IdentityUser<Guid>
{
    public AccountStatus Status { get; private set; } = AccountStatus.Active;

    public void Suspend()
    {
        if (Status == AccountStatus.Closed)
        {
            throw new InvalidOperationException("Closed accounts cannot be suspended.");
        }

        Status = AccountStatus.Suspended;
    }

    public void Close()
    {
        if (Status == AccountStatus.Closed)
        {
            return;
        }

        Status = AccountStatus.Closed;
    }
}
