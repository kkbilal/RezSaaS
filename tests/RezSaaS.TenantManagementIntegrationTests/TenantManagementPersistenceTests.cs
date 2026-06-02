using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

namespace RezSaaS.TenantManagementIntegrationTests;

public sealed class TenantManagementPersistenceTests : IAsyncLifetime
{
    private readonly string databaseName = $"rezsaas_tenant_tests_{Guid.NewGuid():N}";
    private readonly DateTimeOffset testTime =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private string DatabaseConnectionString => CreateDatabaseConnectionString();

    public async Task InitializeAsync()
    {
        await CreateDatabaseAsync();

        await using TenantManagementDbContext dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DropDatabaseAsync();
    }

    [Fact]
    public async Task MigrationDoesNotProvisionTenantData()
    {
        await using TenantManagementDbContext dbContext = CreateDbContext();

        Assert.Equal(0, await dbContext.Tenants.CountAsync());
        Assert.Equal(0, await dbContext.Memberships.CountAsync());
        Assert.Equal(0, await dbContext.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task TenantMembershipAndAuditEntryCanBePersisted()
    {
        Tenant tenant = Tenant.Create(
            slug: $"salon-{Guid.NewGuid():N}"[..16],
            displayName: "Salon Demo",
            createdAtUtc: testTime);
        Guid userAccountId = Guid.CreateVersion7();
        tenant.AddMembership(userAccountId, TenantMembershipRole.BusinessOwner, testTime);
        TenantAuditLogEntry auditLogEntry = TenantAuditLogEntry.Create(
            tenant.Id,
            userAccountId,
            "TenantCreated",
            """{"source":"integration-test"}""",
            testTime);

        await using TenantManagementDbContext dbContext = CreateDbContext();
        dbContext.Tenants.Add(tenant);
        dbContext.AuditLogEntries.Add(auditLogEntry);
        await dbContext.SaveChangesAsync();

        Assert.Equal(1, await dbContext.Tenants.CountAsync());
        Assert.Equal(1, await dbContext.Memberships.CountAsync());
        Assert.Equal(1, await dbContext.AuditLogEntries.CountAsync());
    }

    [Fact]
    public async Task TenantSlugMustBeUniqueCaseInsensitively()
    {
        string slug = $"salon-{Guid.NewGuid():N}"[..16];

        await using TenantManagementDbContext dbContext = CreateDbContext();
        dbContext.Tenants.Add(Tenant.Create(slug, "First Salon", testTime));
        dbContext.Tenants.Add(Tenant.Create(slug.ToUpperInvariant(), "Second Salon", testTime));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task UserCanHaveOnlyOneMembershipPerTenant()
    {
        Tenant tenant = Tenant.Create(
            slug: $"salon-{Guid.NewGuid():N}"[..16],
            displayName: "Salon Demo",
            createdAtUtc: testTime);
        Guid userAccountId = Guid.CreateVersion7();
        tenant.AddMembership(userAccountId, TenantMembershipRole.BusinessOwner, testTime);
        tenant.AddMembership(userAccountId, TenantMembershipRole.Staff, testTime, Guid.CreateVersion7());

        await using TenantManagementDbContext dbContext = CreateDbContext();
        dbContext.Tenants.Add(tenant);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public void BusinessOwnerMembershipCannotBeBranchScoped()
    {
        Assert.Throws<ArgumentException>(() =>
            TenantMembership.Create(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                TenantMembershipRole.BusinessOwner,
                testTime,
                Guid.CreateVersion7()));
    }

    private TenantManagementDbContext CreateDbContext()
    {
        DbContextOptions<TenantManagementDbContext> options =
            new DbContextOptionsBuilder<TenantManagementDbContext>()
                .UseNpgsql(DatabaseConnectionString)
                .Options;

        return new TenantManagementDbContext(options);
    }

    private static string GetAdminConnectionString()
    {
        return Environment.GetEnvironmentVariable("REZSAAS_TEST_POSTGRES_CONNECTION_STRING")
            ?? CreateAdminConnectionStringFromLocalEnvironment()
            ?? throw new InvalidOperationException(
                "Integration tests require either 'REZSAAS_TEST_POSTGRES_CONNECTION_STRING' "
                + "or a local '.env' file at the repository root.");
    }

    private static string? CreateAdminConnectionStringFromLocalEnvironment()
    {
        string? environmentPath = FindEnvironmentPath();

        if (environmentPath is null)
        {
            return null;
        }

        Dictionary<string, string> values = ReadEnvironmentFile(environmentPath);
        NpgsqlConnectionStringBuilder builder = new()
        {
            Host = GetRequiredValue(values, "REZSAAS_POSTGRES_HOST"),
            Port = int.Parse(
                GetRequiredValue(values, "REZSAAS_POSTGRES_PORT"),
                CultureInfo.InvariantCulture),
            Database = "postgres",
            Username = GetRequiredValue(values, "REZSAAS_POSTGRES_USER"),
            Password = GetRequiredValue(values, "REZSAAS_POSTGRES_PASSWORD"),
        };

        return builder.ConnectionString;
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

    private static string? FindEnvironmentPath()
    {
        DirectoryInfo? directory = new(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            string environmentPath = Path.Combine(directory.FullName, ".env");

            if (File.Exists(environmentPath))
            {
                return environmentPath;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string GetRequiredValue(
        Dictionary<string, string> values,
        string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Local '.env' value '{key}' is required for integration tests.");
        }

        return value;
    }

    private static string QuoteIdentifier(string identifier)
    {
        using NpgsqlCommandBuilder builder = new();
        return builder.QuoteIdentifier(identifier);
    }

    private static Dictionary<string, string> ReadEnvironmentFile(string path)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] parts = line.Split('=', 2);

            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new InvalidOperationException($"Invalid local '.env' entry: '{line}'.");
            }

            values[parts[0].Trim()] = TrimOptionalQuotes(parts[1].Trim());
        }

        return values;
    }

    private static string TrimOptionalQuotes(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"')
                || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
