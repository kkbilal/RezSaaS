using Microsoft.AspNetCore.Identity;
using RezSaaS.Modules.Identity.Application;
using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Modules.Identity.Infrastructure.Email;

public sealed class DevelopmentSinkEmailSender : IEmailSender<UserAccount>, IUserTransactionalEmailSender
{
    public Task SendConfirmationLinkAsync(
        UserAccount user,
        string email,
        string confirmationLink)
    {
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(UserAccount user, string email, string resetCode)
    {
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(UserAccount user, string email, string resetLink)
    {
        return Task.CompletedTask;
    }

    public Task SendAsync(
        UserAccount user,
        string email,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
