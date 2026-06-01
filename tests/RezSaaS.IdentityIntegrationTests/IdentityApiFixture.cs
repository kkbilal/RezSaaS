using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityApiFixture : IAsyncLifetime
{
    private const string LocalAdminConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=rezsaas;Password=rezsaas-local-only";

    private readonly string databaseName = $"rezsaas_identity_tests_{Guid.NewGuid():N}";
    private WebApplicationFactory<Program>? factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await CreateDatabaseAsync();

        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:IdentityDatabase"] = CreateDatabaseConnectionString(),
                            ["Identity:AuthenticationPermitLimit"] = "100",
                            ["Identity:EmailDeliveryMode"] = "DevelopmentSink",
                            ["Identity:RequireConfirmedEmail"] = "false",
                        });
                });
            });

        using IServiceScope scope = factory.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();

        Client = factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await DropDatabaseAsync();
    }

    public async Task SuspendUserAsync(string email)
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        UserManager<UserAccount> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
        UserAccount user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"User '{email}' was not found.");

        user.Suspend();

        IdentityResult result = await userManager.UpdateAsync(user);
        Assert.True(result.Succeeded);
    }

    public async Task<bool> PlatformRoleExistsAsync(string roleName)
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        RoleManager<IdentityRole<Guid>> roleManager =
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        return await roleManager.RoleExistsAsync(roleName);
    }

    public HttpClient CreateClient()
    {
        return factory!.CreateClient();
    }

    private static string GetAdminConnectionString()
    {
        return Environment.GetEnvironmentVariable("REZSAAS_TEST_POSTGRES_CONNECTION_STRING")
            ?? LocalAdminConnectionString;
    }

    private string CreateDatabaseConnectionString()
    {
        NpgsqlConnectionStringBuilder builder = new(GetAdminConnectionString())
        {
            Database = databaseName,
        };

        return builder.ConnectionString;
    }

    private async Task CreateDatabaseAsync()
    {
        await using NpgsqlConnection connection = new(GetAdminConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync()
    {
        await using NpgsqlConnection connection = new(GetAdminConnectionString());
        await connection.OpenAsync();

        await using (NpgsqlCommand terminateConnections = connection.CreateCommand())
        {
            terminateConnections.CommandText =
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = $1";
            terminateConnections.Parameters.AddWithValue(databaseName);
            await terminateConnections.ExecuteNonQueryAsync();
        }

        await using NpgsqlCommand dropDatabase = connection.CreateCommand();
        dropDatabase.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)}";
        await dropDatabase.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string identifier)
    {
        using NpgsqlCommandBuilder builder = new();
        return builder.QuoteIdentifier(identifier);
    }
}
