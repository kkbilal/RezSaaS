using Microsoft.AspNetCore.Authorization;

namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed class PlatformStepUpRequirement : IAuthorizationRequirement
{
    public PlatformStepUpRequirement(string method)
    {
        Method = method;
    }

    public string Method { get; }
}
