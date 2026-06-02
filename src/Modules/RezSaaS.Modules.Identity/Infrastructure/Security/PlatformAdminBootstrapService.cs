using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Identity.Configuration;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed class PlatformAdminBootstrapService : IPlatformAdminBootstrapService
{
    private readonly IdentityDbContext dbContext;
    private readonly PlatformAdminBootstrapOptions options;
    private readonly RoleManager<IdentityRole<Guid>> roleManager;
    private readonly TimeProvider timeProvider;
    private readonly UserManager<UserAccount> userManager;

    public PlatformAdminBootstrapService(
        IdentityDbContext dbContext,
        IOptions<PlatformAdminBootstrapOptions> options,
        RoleManager<IdentityRole<Guid>> roleManager,
        TimeProvider timeProvider,
        UserManager<UserAccount> userManager)
    {
        this.dbContext = dbContext;
        this.options = options.Value;
        this.roleManager = roleManager;
        this.timeProvider = timeProvider;
        this.userManager = userManager;
    }

    public async Task<PlatformAdminBootstrapResult> BootstrapAsync(
        PlatformAdminBootstrapRequest request,
        CancellationToken cancellationToken = default)
    {
        options.ValidateForBootstrap();

        if (!TokenMatches(request.BootstrapToken))
        {
            return PlatformAdminBootstrapResult.Failed("Invalid bootstrap token.");
        }

        if (await PlatformAdminExistsAsync(cancellationToken))
        {
            return PlatformAdminBootstrapResult.Failed("A platform administrator already exists.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        IdentityResult roleResult = await EnsurePlatformRoleAsync(PlatformRoleNames.Administrator);

        if (!roleResult.Succeeded)
        {
            return PlatformAdminBootstrapResult.Failed("Platform admin role could not be created.");
        }

        IdentityResult supportRoleResult = await EnsurePlatformRoleAsync(PlatformRoleNames.Support);

        if (!supportRoleResult.Succeeded)
        {
            return PlatformAdminBootstrapResult.Failed("Platform support role could not be created.");
        }

        UserAccount user = new()
        {
            Email = request.Email,
            EmailConfirmed = true,
            UserName = request.Email,
        };

        IdentityResult createUserResult = await userManager.CreateAsync(user, request.Password);

        if (!createUserResult.Succeeded)
        {
            return PlatformAdminBootstrapResult.Failed("Platform admin user could not be created.");
        }

        IdentityResult addRoleResult = await userManager.AddToRoleAsync(user, PlatformRoleNames.Administrator);

        if (!addRoleResult.Succeeded)
        {
            return PlatformAdminBootstrapResult.Failed("Platform admin role could not be assigned.");
        }

        dbContext.IdentityAuditLogEntries.Add(
            IdentityAuditLogEntry.Create(
                actorUserAccountId: null,
                subjectUserAccountId: user.Id,
                action: "PlatformAdminBootstrapped",
                detailsJson: "{}",
                occurredAtUtc: timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return PlatformAdminBootstrapResult.Success();
    }

    private async Task<IdentityResult> EnsurePlatformRoleAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return IdentityResult.Success;
        }

        return await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
    }

    private async Task<bool> PlatformAdminExistsAsync(CancellationToken cancellationToken)
    {
        if (!await roleManager.RoleExistsAsync(PlatformRoleNames.Administrator))
        {
            return false;
        }

        return await dbContext.UserRoles
            .Join(
                dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (_, role) => role.Name)
            .AnyAsync(roleName => roleName == PlatformRoleNames.Administrator, cancellationToken);
    }

    private bool TokenMatches(string providedToken)
    {
        byte[] configured = Convert.FromHexString(options.PlatformAdminBootstrapTokenSha256);
        byte[] provided = SHA256.HashData(Encoding.UTF8.GetBytes(providedToken));

        return configured.Length == provided.Length
            && CryptographicOperations.FixedTimeEquals(configured, provided);
    }
}
