using Microsoft.AspNetCore.Identity;
using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Modules.Identity.Infrastructure.Email;

public sealed class UnconfiguredEmailSender : IEmailSender<UserAccount>
{
    public Task SendConfirmationLinkAsync(
        UserAccount user,
        string email,
        string confirmationLink)
    {
        return NotConfigured();
    }

    public Task SendPasswordResetCodeAsync(UserAccount user, string email, string resetCode)
    {
        return NotConfigured();
    }

    public Task SendPasswordResetLinkAsync(UserAccount user, string email, string resetLink)
    {
        return NotConfigured();
    }

    private static Task NotConfigured()
    {
        return Task.FromException(
            new InvalidOperationException("An email provider must be configured before email delivery is enabled."));
    }
}
