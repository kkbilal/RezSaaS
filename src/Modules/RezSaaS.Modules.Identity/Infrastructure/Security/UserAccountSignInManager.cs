using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed class UserAccountSignInManager : SignInManager<UserAccount>
{
    public UserAccountSignInManager(
        UserManager<UserAccount> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<UserAccount> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<UserAccount>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<UserAccount> confirmation)
        : base(
            userManager,
            contextAccessor,
            claimsFactory,
            optionsAccessor,
            logger,
            schemes,
            confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(UserAccount user)
    {
        return user.Status == AccountStatus.Active && await base.CanSignInAsync(user);
    }
}
