using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using RezSaaS.Modules.Identity.Configuration;
using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Modules.Identity.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender<UserAccount>
{
    private readonly IdentitySmtpEmailOptions options;

    public SmtpEmailSender(IdentitySmtpEmailOptions options)
    {
        this.options = options;
    }

    public Task SendConfirmationLinkAsync(
        UserAccount user,
        string email,
        string confirmationLink)
    {
        return SendAsync(email, "RezSaaS e-posta doğrulama", confirmationLink);
    }

    public Task SendPasswordResetCodeAsync(UserAccount user, string email, string resetCode)
    {
        return SendAsync(email, "RezSaaS parola sıfırlama kodu", resetCode);
    }

    public Task SendPasswordResetLinkAsync(UserAccount user, string email, string resetLink)
    {
        return SendAsync(email, "RezSaaS parola sıfırlama", resetLink);
    }

    private async Task SendAsync(string recipientEmail, string subject, string body)
    {
        using MailMessage message = new()
        {
            From = new MailAddress(options.FromAddress, options.FromName),
            Subject = subject,
            Body = body,
        };
        message.To.Add(recipientEmail);

        using SmtpClient client = new(options.Host, options.Port)
        {
            EnableSsl = options.UseSsl,
        };

        if (!string.IsNullOrWhiteSpace(options.UserName))
        {
            client.Credentials = new NetworkCredential(options.UserName, options.Password);
        }

        await client.SendMailAsync(message);
    }
}
