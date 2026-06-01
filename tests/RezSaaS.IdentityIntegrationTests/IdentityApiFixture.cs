using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityApiFixture : IAsyncLifetime
{
    private readonly string databaseName = $"rezsaas_identity_tests_{Guid.NewGuid():N}";
    private WebApplicationFactory<Program>? factory;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await CreateDatabaseAsync();
        string databaseConnectionString = CreateDatabaseConnectionString();

        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:IdentityDatabase"] = databaseConnectionString,
                            ["Identity:AuthenticationPermitLimit"] = "100",
                            ["Identity:AuthenticationWindowMinutes"] = "1",
                            ["Identity:DeliveryMode"] = "DevelopmentSink",
                            ["Identity:LockoutMinutes"] = "15",
                            ["Identity:MaxFailedAccessAttempts"] = "5",
                            ["Identity:PasswordRequiredLength"] = "12",
                            ["Identity:PasswordRequiredUniqueChars"] = "4",
                            ["Identity:RequireConfirmedEmail"] = "false",
                        });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                    services.AddDbContext<IdentityDbContext>(
                        options => options.UseNpgsql(databaseConnectionString));
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

    public HttpClient CreateClient()
    {
        return factory!.CreateClient();
    }

    public async Task<int> GetPlatformRoleCountAsync()
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        return await dbContext.Roles.CountAsync();
    }

    private static string GetAdminConnectionString()
    {
        return Environment.GetEnvironmentVariable("REZSAAS_TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Environment variable 'REZSAAS_TEST_POSTGRES_CONNECTION_STRING' is required. "
                + "Run '. .\\scripts\\Import-LocalEnvironment.ps1' before executing integration tests.");
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
