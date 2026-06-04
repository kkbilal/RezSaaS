using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Modules.Identity.Application;

public interface IUserTransactionalEmailSender
{
    Task SendAsync(
        UserAccount user,
        string email,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
