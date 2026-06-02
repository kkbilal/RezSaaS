using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RezSaaS.BuildingBlocks.Security;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;
using RezSaaS.Modules.Availability.Infrastructure.Persistence;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;
using RezSaaS.Modules.Messaging.Infrastructure.Persistence;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;

namespace RezSaaS.Phase1CoreIntegrationTests;

public sealed class Phase1CorePersistenceTests : IAsyncLifetime
{
    private readonly string databaseName = $"rezsaas_phase1_tests_{Guid.NewGuid():N}";
    private readonly DateTimeOffset testTime =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private string DatabaseConnectionString => CreateDatabaseConnectionString();

    public async Task InitializeAsync()
    {
        await CreateDatabaseAsync();
        await MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DropDatabaseAsync();
    }

    [Fact]
    public async Task MigrationsDoNotProvisionMutablePhase1Data()
    {
        Assert.Equal(0, await CountRowsAsync("organization", "Businesses"));
        Assert.Equal(0, await CountRowsAsync("admin", "AbuseEvents"));
        Assert.Equal(0, await CountRowsAsync("catalog", "Services"));
        Assert.Equal(0, await CountRowsAsync("messaging", "TransactionalMessages"));
        Assert.Equal(0, await CountRowsAsync("resources", "Resources"));
        Assert.Equal(0, await CountRowsAsync("availability", "BranchWorkingHours"));
        Assert.Equal(0, await CountRowsAsync("booking", "AppointmentRequests"));
        Assert.Equal(0, await CountRowsAsync("booking", "Appointments"));
    }

    [Fact]
    public async Task TenantQueryFilterShowsOnlyCurrentTenantData()
    {
        Guid tenantA = Guid.CreateVersion7();
        Guid tenantB = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantA,
        };

        await using OrganizationDbContext dbContext =
            new(CreateOptions<OrganizationDbContext>(), tenantContextAccessor);
        dbContext.Businesses.Add(Business.Create(tenantA, "tenant-a", "Tenant A", "hair", testTime));
        dbContext.Businesses.Add(Business.Create(tenantB, "tenant-b", "Tenant B", "hair", testTime));
        await dbContext.SaveChangesAsync();

        Assert.Equal(1, await dbContext.Businesses.CountAsync());

        tenantContextAccessor.TenantId = tenantB;
        Assert.Equal(1, await dbContext.Businesses.CountAsync());

        tenantContextAccessor.TenantId = null;
        Assert.Equal(0, await dbContext.Businesses.CountAsync());
    }

    [Fact]
    public void PiiMaskerKeepsOperationalLogsFromExposingRawContactData()
    {
        Assert.Equal("b***@example.test", PiiMasker.MaskEmail("bilal@example.test"));
        Assert.Equal("***7890", PiiMasker.MaskPhone("+90 555 123 7890"));
    }

    [Fact]
    public void AppointmentRequestExpiryUsesEarliestOfTwentyFourHoursAndResponseBuffer()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerUserAccountId = Guid.CreateVersion7();
        Guid branchId = Guid.CreateVersion7();
        Guid staffMemberId = Guid.CreateVersion7();
        Guid resourceId = Guid.CreateVersion7();

        AppointmentRequest longRangeRequest = AppointmentRequest.Create(
            tenantId,
            customerUserAccountId,
            branchId,
            staffMemberId,
            resourceId,
            testTime.AddDays(2),
            testTime.AddDays(2).AddHours(1),
            testTime,
            TimeSpan.FromHours(2));

        AppointmentRequest nearRequest = AppointmentRequest.Create(
            tenantId,
            customerUserAccountId,
            branchId,
            staffMemberId,
            resourceId,
            testTime.AddHours(3),
            testTime.AddHours(4),
            testTime,
            TimeSpan.FromHours(2));

        Assert.Equal(testTime.AddHours(24), longRangeRequest.ExpiresAtUtc);
        Assert.Equal(testTime.AddHours(1), nearRequest.ExpiresAtUtc);
    }

    [Fact]
    public async Task PendingRequestsDoNotBlockSameSlot()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerUserAccountId = Guid.CreateVersion7();
        Guid branchId = Guid.CreateVersion7();
        Guid staffMemberId = Guid.CreateVersion7();
        Guid resourceId = Guid.CreateVersion7();

        await using BookingDbContext dbContext = CreateBookingDbContext();
        dbContext.AppointmentRequests.Add(CreateRequest(
            tenantId,
            customerUserAccountId,
            branchId,
            staffMemberId,
            resourceId));
        dbContext.AppointmentRequests.Add(CreateRequest(
            tenantId,
            Guid.CreateVersion7(),
            branchId,
            staffMemberId,
            resourceId));

        await dbContext.SaveChangesAsync();

        Assert.Equal(2, await CountRowsAsync("booking", "AppointmentRequests"));
    }

    [Fact]
    public async Task ConfirmedAppointmentsCannotOverlapForSameStaffOrSameResource()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid branchId = Guid.CreateVersion7();
        Guid staffMemberId = Guid.CreateVersion7();
        Guid resourceId = Guid.CreateVersion7();

        await SaveAppointmentAsync(
            tenantId,
            branchId,
            staffMemberId,
            resourceId,
            testTime,
            testTime.AddHours(1));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveAppointmentAsync(
                tenantId,
                branchId,
                staffMemberId,
                Guid.CreateVersion7(),
                testTime.AddMinutes(30),
                testTime.AddMinutes(90)));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            SaveAppointmentAsync(
                tenantId,
                branchId,
                Guid.CreateVersion7(),
                resourceId,
                testTime.AddMinutes(30),
                testTime.AddMinutes(90)));
    }

    [Fact]
    public async Task ConfirmedAppointmentOverlapConstraintIsTenantScoped()
    {
        Guid staffMemberId = Guid.CreateVersion7();
        Guid resourceId = Guid.CreateVersion7();

        await SaveAppointmentAsync(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            staffMemberId,
            resourceId,
            testTime,
            testTime.AddHours(1));

        await SaveAppointmentAsync(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            staffMemberId,
            resourceId,
            testTime.AddMinutes(30),
            testTime.AddMinutes(90));

        Assert.Equal(2, await CountRowsAsync("booking", "Appointments"));
    }

    private AppointmentRequest CreateRequest(
        Guid tenantId,
        Guid customerUserAccountId,
        Guid branchId,
        Guid staffMemberId,
        Guid resourceId)
    {
        AppointmentRequest request = AppointmentRequest.Create(
            tenantId,
            customerUserAccountId,
            branchId,
            staffMemberId,
            resourceId,
            testTime.AddDays(1),
            testTime.AddDays(1).AddHours(1),
            testTime,
            TimeSpan.FromHours(2));
        request.AddLine(Guid.CreateVersion7(), "Saç Kesimi", 60, 500, "TRY");

        return request;
    }

    private BookingDbContext CreateBookingDbContext()
    {
        return new BookingDbContext(CreateOptions<BookingDbContext>());
    }

    private DbContextOptions<TContext> CreateOptions<TContext>()
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(DatabaseConnectionString)
            .Options;
    }

    private async Task<int> CountRowsAsync(string schema, string table)
    {
        await using NpgsqlConnection connection = new(DatabaseConnectionString);
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(schema)}.{QuoteIdentifier(table)}";

        object? value = await command.ExecuteScalarAsync();
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
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

    private async Task MigrateAsync()
    {
        await using (AdminDbContext dbContext =
            new(CreateOptions<AdminDbContext>()))
        {
            await dbContext.Database.MigrateAsync();
        }

        await using (OrganizationDbContext dbContext =
            new(CreateOptions<OrganizationDbContext>()))
        {
            await dbContext.Database.MigrateAsync();
        }

        await using (CatalogDbContext dbContext =
            new(CreateOptions<CatalogDbContext>()))
        {
            await dbContext.Database.MigrateAsync();
        }

        await using (MessagingDbContext dbContext =
            new(CreateOptions<MessagingDbContext>()))
        {
            await dbContext.Database.MigrateAsync();
        }

        await using (ResourcesDbContext dbContext =
            new(CreateOptions<ResourcesDbContext>()))
        {
            await dbContext.Database.MigrateAsync();
        }

        await using (AvailabilityDbContext dbContext =
            new(CreateOptions<AvailabilityDbContext>()))
        {
            await dbContext.Database.MigrateAsync();
        }

        await using (BookingDbContext dbContext =
            new(CreateOptions<BookingDbContext>()))
        {
            await dbContext.Database.MigrateAsync();
        }
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

    private async Task SaveAppointmentAsync(
        Guid tenantId,
        Guid branchId,
        Guid staffMemberId,
        Guid resourceId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        Appointment appointment = Appointment.CreateConfirmed(
            tenantId,
            appointmentRequestId: null,
            Guid.CreateVersion7(),
            branchId,
            staffMemberId,
            resourceId,
            startUtc,
            endUtc,
            testTime);
        appointment.AddLine(Guid.CreateVersion7(), "Saç Kesimi", 60, 500, "TRY");

        await using BookingDbContext dbContext = CreateBookingDbContext();
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync();
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
