using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RezSaaS.Modules.Identity;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityConfigurationTests
{
    [Fact]
    public void ProductionEmailConfirmationRequiresConfiguredDelivery()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:IdentityDatabase"] = "Host=localhost;Database=rezsaas",
                    ["Identity:AuthenticationPermitLimit"] = "10",
                    ["Identity:AuthenticationWindowMinutes"] = "1",
                    ["Identity:DeliveryMode"] = "Unconfigured",
                    ["Identity:LockoutMinutes"] = "15",
                    ["Identity:MaxFailedAccessAttempts"] = "5",
                    ["Identity:PasswordRequiredLength"] = "12",
                    ["Identity:PasswordRequiredUniqueChars"] = "4",
                    ["Identity:RequireConfirmedEmail"] = "true",
                })
            .Build();
        ServiceCollection services = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new IdentityModule().AddServices(services, configuration));

        Assert.Contains("email provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
