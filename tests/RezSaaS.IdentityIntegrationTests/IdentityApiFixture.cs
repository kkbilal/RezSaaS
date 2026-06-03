using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Availability.Domain;
using RezSaaS.Modules.Availability.Infrastructure.Persistence;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;
using RezSaaS.Modules.Catalog.Domain;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;
using RezSaaS.Modules.Identity.Infrastructure.Security;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;
using RezSaaS.Modules.Resources.Domain;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;

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
                builder.UseSetting("Identity:AuthenticationPermitLimit", "100");
                builder.UseSetting("Identity:AuthenticationWindowMinutes", "1");
                builder.UseSetting("Identity:DeliveryMode", "DevelopmentSink");
                builder.UseSetting("Identity:LockoutMinutes", "15");
                builder.UseSetting("Identity:MaxFailedAccessAttempts", "5");
                builder.UseSetting("Identity:PasswordRequiredLength", "12");
                builder.UseSetting("Identity:PasswordRequiredUniqueChars", "4");
                builder.UseSetting("Identity:RequireConfirmedEmail", "false");
                builder.UseSetting(
                    "Identity:Bootstrap:PlatformAdminBootstrapTokenSha256",
                    "99ECD312D2F24FFD7011532BA5579DAE00103767862BD5B7A79E6EFCEF99E05E");
                builder.UseSetting("ConnectionStrings:AdminDatabase", databaseConnectionString);
                builder.UseSetting("ConnectionStrings:AvailabilityDatabase", databaseConnectionString);
                builder.UseSetting("ConnectionStrings:BookingDatabase", databaseConnectionString);
                builder.UseSetting("ConnectionStrings:CatalogDatabase", databaseConnectionString);
                builder.UseSetting("ConnectionStrings:IdentityDatabase", databaseConnectionString);
                builder.UseSetting("ConnectionStrings:MessagingDatabase", databaseConnectionString);
                builder.UseSetting("ConnectionStrings:OrganizationDatabase", databaseConnectionString);
                builder.UseSetting("ConnectionStrings:ResourcesDatabase", databaseConnectionString);
                builder.UseSetting("ConnectionStrings:TenantManagementDatabase", databaseConnectionString);
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:AdminDatabase"] = databaseConnectionString,
                            ["ConnectionStrings:AvailabilityDatabase"] = databaseConnectionString,
                            ["ConnectionStrings:BookingDatabase"] = databaseConnectionString,
                            ["ConnectionStrings:CatalogDatabase"] = databaseConnectionString,
                            ["ConnectionStrings:IdentityDatabase"] = databaseConnectionString,
                            ["ConnectionStrings:MessagingDatabase"] = databaseConnectionString,
                            ["ConnectionStrings:OrganizationDatabase"] = databaseConnectionString,
                            ["ConnectionStrings:ResourcesDatabase"] = databaseConnectionString,
                            ["ConnectionStrings:TenantManagementDatabase"] = databaseConnectionString,
                            ["Identity:AuthenticationPermitLimit"] = "100",
                            ["Identity:AuthenticationWindowMinutes"] = "1",
                            ["Identity:DeliveryMode"] = "DevelopmentSink",
                            ["Identity:LockoutMinutes"] = "15",
                            ["Identity:MaxFailedAccessAttempts"] = "5",
                            ["Identity:PasswordRequiredLength"] = "12",
                            ["Identity:PasswordRequiredUniqueChars"] = "4",
                            ["Identity:RequireConfirmedEmail"] = "false",
                            ["Identity:Bootstrap:PlatformAdminBootstrapTokenSha256"] =
                                "99ECD312D2F24FFD7011532BA5579DAE00103767862BD5B7A79E6EFCEF99E05E",
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
        await MigratePublicProfileContextsAsync(scope.ServiceProvider);

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

    public async Task<PublicBusinessProfileSeed> SeedPublicBusinessProfileAsync()
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        ITenantContextAccessor tenantContextAccessor =
            scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        OrganizationDbContext organizationDbContext =
            scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        CatalogDbContext catalogDbContext =
            scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        AvailabilityDbContext availabilityDbContext =
            scope.ServiceProvider.GetRequiredService<AvailabilityDbContext>();
        ResourcesDbContext resourcesDbContext =
            scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
        BookingDbContext bookingDbContext =
            scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        Guid tenantId = Guid.CreateVersion7();
        DateTimeOffset createdAtUtc = new(2026, 1, 2, 9, 0, 0, TimeSpan.Zero);
        string slug = $"atlas-{Guid.NewGuid():N}"[..22];
        string branchSlug = "kadikoy";
        tenantContextAccessor.TenantId = tenantId;

        Business business = Business.Create(
            tenantId,
            slug,
            "Atlas Hair",
            "hair",
            createdAtUtc,
            "Kadikoy'de modern sac ve bakim salonu.");
        Branch branch = Branch.Create(
            tenantId,
            business.Id,
            branchSlug,
            "Kadikoy",
            "Europe/Istanbul",
            createdAtUtc,
            "Istanbul",
            "Kadikoy",
            "Caferaga Mahallesi");
        StaffMember staffMember = StaffMember.Create(
            tenantId,
            branch.Id,
            "Ayse Usta",
            createdAtUtc);

        organizationDbContext.Businesses.Add(business);
        organizationDbContext.Branches.Add(branch);
        organizationDbContext.StaffMembers.Add(staffMember);
        await organizationDbContext.SaveChangesAsync();

        ResourceType chairType = ResourceType.Create(tenantId, "chair", "Chair");
        Resource chair = Resource.Create(tenantId, branch.Id, chairType.Id, "Chair 1");
        ResourceType roomType = ResourceType.Create(tenantId, "room", "Room");
        Resource room = Resource.Create(tenantId, branch.Id, roomType.Id, "Room 1");
        resourcesDbContext.ResourceTypes.AddRange(chairType, roomType);
        resourcesDbContext.Resources.AddRange(chair, room);
        await resourcesDbContext.SaveChangesAsync();

        Service service = Service.Create(
            tenantId,
            "Sac Kesimi",
            "hair",
            createdAtUtc);
        ServiceVariant variant = ServiceVariant.Create(
            tenantId,
            service.Id,
            "Standart Kesim",
            45,
            750,
            "TRY",
            createdAtUtc,
            chairType.Id);

        catalogDbContext.Services.Add(service);
        catalogDbContext.ServiceVariants.Add(variant);
        await catalogDbContext.SaveChangesAsync();

        availabilityDbContext.BranchWorkingHours.Add(
            BranchWorkingHours.Create(
                tenantId,
                branch.Id,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0)));
        availabilityDbContext.StaffUnavailableTimes.Add(
            StaffUnavailableTime.Create(
                tenantId,
                staffMember.Id,
                new DateTimeOffset(2026, 1, 5, 6, 45, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 5, 7, 30, 0, TimeSpan.Zero),
                "Break"));
        await availabilityDbContext.SaveChangesAsync();

        Appointment confirmedAppointment = Appointment.CreateConfirmed(
            tenantId,
            null,
            Guid.CreateVersion7(),
            branch.Id,
            staffMember.Id,
            chair.Id,
            new DateTimeOffset(2026, 1, 5, 6, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 5, 6, 45, 0, TimeSpan.Zero),
            createdAtUtc);
        confirmedAppointment.AddLine(
            variant.Id,
            service.Name,
            variant.DurationMinutes,
            variant.PriceAmount,
            variant.CurrencyCode);
        bookingDbContext.Appointments.Add(confirmedAppointment);
        await bookingDbContext.SaveChangesAsync();

        resourcesDbContext.ResourceBlocks.Add(
            ResourceBlock.Create(
                tenantId,
                chair.Id,
                new DateTimeOffset(2026, 1, 5, 7, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 5, 8, 15, 0, TimeSpan.Zero),
                "Maintenance"));
        await resourcesDbContext.SaveChangesAsync();

        tenantContextAccessor.TenantId = null;

        return new PublicBusinessProfileSeed(
            slug,
            branchSlug,
            variant.Id,
            staffMember.Id);
    }

    public async Task<PlatformAdminBootstrapResult> BootstrapPlatformAdminAsync(
        string email,
        string password,
        string token)
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        IPlatformAdminBootstrapService service =
            scope.ServiceProvider.GetRequiredService<IPlatformAdminBootstrapService>();

        return await service.BootstrapAsync(
            new PlatformAdminBootstrapRequest(email, password, token));
    }

    public async Task<int> GetIdentityAuditLogCountAsync()
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        return await dbContext.IdentityAuditLogEntries.CountAsync();
    }

    public async Task<int> GetPlatformAdminAssignmentCountAsync()
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        return await dbContext.UserRoles
            .Join(
                dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (_, role) => role.Name)
            .CountAsync(roleName => roleName == PlatformRoleNames.Administrator);
    }

    public async Task<int> GetPlatformRoleCountAsync()
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        return await dbContext.Roles.CountAsync();
    }

    private static async Task MigratePublicProfileContextsAsync(IServiceProvider serviceProvider)
    {
        OrganizationDbContext organizationDbContext =
            serviceProvider.GetRequiredService<OrganizationDbContext>();
        CatalogDbContext catalogDbContext =
            serviceProvider.GetRequiredService<CatalogDbContext>();
        AvailabilityDbContext availabilityDbContext =
            serviceProvider.GetRequiredService<AvailabilityDbContext>();
        ResourcesDbContext resourcesDbContext =
            serviceProvider.GetRequiredService<ResourcesDbContext>();
        BookingDbContext bookingDbContext =
            serviceProvider.GetRequiredService<BookingDbContext>();

        await organizationDbContext.Database.MigrateAsync();
        await catalogDbContext.Database.MigrateAsync();
        await availabilityDbContext.Database.MigrateAsync();
        await resourcesDbContext.Database.MigrateAsync();
        await bookingDbContext.Database.MigrateAsync();
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
