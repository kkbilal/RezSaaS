using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Identity.Configuration;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed class StepUpSessionService
{
    public const string MethodMfa = "mfa";
    public const string MethodPassword = "pwd";

    private const int TokenByteLength = 32;

    private readonly IdentityDbContext dbContext;
    private readonly StepUpSessionOptions options;
    private readonly TimeProvider timeProvider;
    private readonly UserManager<UserAccount> userManager;

    public StepUpSessionService(
        IdentityDbContext dbContext,
        IOptions<StepUpSessionOptions> options,
        TimeProvider timeProvider,
        UserManager<UserAccount> userManager)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.timeProvider = timeProvider;
        this.userManager = userManager;
    }

    public async Task<StepUpSessionResult> CreateAsync(
        Guid userAccountId,
        string password,
        string? twoFactorCode,
        string? recoveryCode,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty || string.IsNullOrWhiteSpace(password))
        {
            return StepUpSessionResult.Failure("STEP_UP_INVALID_REQUEST");
        }

        UserAccount? user = await userManager.FindByIdAsync(userAccountId.ToString());

        if (user is null || user.Status != AccountStatus.Active)
        {
            return StepUpSessionResult.Failure("STEP_UP_USER_NOT_FOUND");
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            return StepUpSessionResult.Failure("STEP_UP_INVALID_CREDENTIALS");
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        bool privilegedAccount = roles.Any(role =>
            string.Equals(role, PlatformRoleNames.Administrator, StringComparison.Ordinal)
            || string.Equals(role, PlatformRoleNames.Support, StringComparison.Ordinal));

        string method = MethodPassword;

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            if (!string.IsNullOrWhiteSpace(recoveryCode))
            {
                IdentityResult recoveryResult =
                    await userManager.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode.Trim());

                if (!recoveryResult.Succeeded)
                {
                    return StepUpSessionResult.Failure("STEP_UP_INVALID_TWO_FACTOR_CODE");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(twoFactorCode))
                {
                    return StepUpSessionResult.Failure("STEP_UP_TWO_FACTOR_CODE_REQUIRED");
                }

                bool verified = await userManager.VerifyTwoFactorTokenAsync(
                    user,
                    TokenOptions.DefaultAuthenticatorProvider,
                    twoFactorCode.Trim());

                if (!verified)
                {
                    return StepUpSessionResult.Failure("STEP_UP_INVALID_TWO_FACTOR_CODE");
                }
            }

            method = MethodMfa;
        }
        else if (privilegedAccount)
        {
            return StepUpSessionResult.Failure("STEP_UP_MFA_REQUIRED");
        }

        return await PersistSessionAsync(user.Id, method, cancellationToken);
    }

    public async Task<StepUpSessionView?> ValidateAsync(
        Guid userAccountId,
        string? rawToken,
        string requiredMethod,
        CancellationToken cancellationToken = default)
    {
        if (userAccountId == Guid.Empty
            || string.IsNullOrWhiteSpace(rawToken)
            || string.IsNullOrWhiteSpace(requiredMethod))
        {
            return null;
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        string tokenHash = HashToken(rawToken);

        StepUpSession? session = await dbContext.StepUpSessions
            .AsNoTracking()
            .Where(entity => entity.UserAccountId == userAccountId
                && entity.TokenHash == tokenHash
                && entity.Method == requiredMethod
                && entity.RevokedAtUtc == null
                && entity.ExpiresAtUtc > now)
            .OrderByDescending(entity => entity.ExpiresAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return session is null
            ? null
            : new StepUpSessionView(
                session.Id,
                session.UserAccountId,
                session.Method,
                session.ExpiresAtUtc);
    }

    private async Task<StepUpSessionResult> PersistSessionAsync(
        Guid userAccountId,
        string method,
        CancellationToken cancellationToken)
    {
        string token = CreateToken();
        string tokenHash = HashToken(token);
        DateTimeOffset createdAtUtc = timeProvider.GetUtcNow();
        DateTimeOffset expiresAtUtc = createdAtUtc.AddMinutes(options.DurationMinutes);
        StepUpSession session = StepUpSession.Create(
            userAccountId,
            tokenHash,
            method,
            createdAtUtc,
            expiresAtUtc);

        dbContext.StepUpSessions.Add(session);
        dbContext.IdentityAuditLogEntries.Add(
            IdentityAuditLogEntry.Create(
                actorUserAccountId: userAccountId,
                subjectUserAccountId: userAccountId,
                action: "StepUpSessionCreated",
                detailsJson: $$"""{"method":"{{method}}","expiresAtUtc":"{{expiresAtUtc:O}}"}""",
                occurredAtUtc: createdAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);

        return StepUpSessionResult.Success(
            token,
            new StepUpSessionView(
                session.Id,
                session.UserAccountId,
                session.Method,
                session.ExpiresAtUtc));
    }

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[TokenByteLength];
        RandomNumberGenerator.Fill(bytes);

        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string rawToken)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToHexString(hash);
    }
}
