using Microsoft.EntityFrameworkCore;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.Modules.Identity.Application;

public sealed class UserTransactionalEmailService
{
    private const string EmailMissing = "USER_TRANSACTIONAL_EMAIL_MISSING";
    private const string InvalidRequest = "USER_TRANSACTIONAL_EMAIL_INVALID";
    private const int MaxBodyLength = 4000;
    private const int MaxSubjectLength = 200;
    private const string UserNotFound = "USER_TRANSACTIONAL_EMAIL_USER_NOT_FOUND";

    private readonly IdentityDbContext dbContext;
    private readonly IUserTransactionalEmailSender emailSender;

    public UserTransactionalEmailService(
        IdentityDbContext dbContext,
        IUserTransactionalEmailSender emailSender)
    {
        this.dbContext = dbContext;
        this.emailSender = emailSender;
    }

    public async Task<UserTransactionalEmailResult> SendAsync(
        Guid userAccountId,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty
            || string.IsNullOrWhiteSpace(subject)
            || subject.Trim().Length > MaxSubjectLength
            || string.IsNullOrWhiteSpace(body)
            || body.Trim().Length > MaxBodyLength)
        {
            return UserTransactionalEmailResult.Failure(InvalidRequest);
        }

        UserAccount? user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == userAccountId, cancellationToken);

        if (user is null)
        {
            return UserTransactionalEmailResult.Failure(UserNotFound);
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return UserTransactionalEmailResult.Failure(EmailMissing);
        }

        await emailSender.SendAsync(
            user,
            user.Email,
            subject.Trim(),
            body.Trim(),
            cancellationToken);

        return UserTransactionalEmailResult.Success();
    }
}
