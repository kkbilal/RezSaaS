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
                    ["Identity:EmailDeliveryMode"] = "Unconfigured",
                    ["Identity:RequireConfirmedEmail"] = "true",
                })
            .Build();
        ServiceCollection services = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new IdentityModule().AddServices(services, configuration));

        Assert.Contains("email provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
