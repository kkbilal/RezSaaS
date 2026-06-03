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
using RezSaaS.Modules.Admin.Infrastructure.Persistence;
using RezSaaS.Modules.Availability.Domain;
using RezSaaS.Modules.Availability.Infrastructure.Persistence;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;
using RezSaaS.Modules.Catalog.Domain;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Identity.Infrastructure.Persistence;
using RezSaaS.Modules.Identity.Infrastructure.Security;
using RezSaaS.Modules.Messaging.Infrastructure.Persistence;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;
using RezSaaS.Modules.Resources.Domain;
using RezSaaS.Modules.Resources.Infrastructure.Persistence;
using RezSaaS.Modules.TenantManagement.Domain;
using RezSaaS.Modules.TenantManagement.Infrastructure.Persistence;

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

    public async Task<PublicBusinessProfileSeed> SeedPublicBusinessProfileAsync(
        DateOnly? slotDate = null,
        bool includeUnqualifiedStaff = false)
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
        TenantManagementDbContext tenantManagementDbContext =
            scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();
        Guid tenantId = Guid.CreateVersion7();
        DateTimeOffset createdAtUtc = new(2026, 1, 2, 9, 0, 0, TimeSpan.Zero);
        DateOnly effectiveSlotDate = slotDate ?? new DateOnly(2026, 1, 5);
        string slug = $"atlas-{Guid.NewGuid():N}"[..22];
        string branchSlug = "kadikoy";
        DateTimeOffset confirmedStartUtc = CreateTurkeyUtc(effectiveSlotDate, new TimeOnly(9, 0));
        DateTimeOffset confirmedEndUtc = CreateTurkeyUtc(effectiveSlotDate, new TimeOnly(9, 45));
        DateTimeOffset unavailableStartUtc = CreateTurkeyUtc(effectiveSlotDate, new TimeOnly(9, 45));
        DateTimeOffset unavailableEndUtc = CreateTurkeyUtc(effectiveSlotDate, new TimeOnly(10, 30));
        DateTimeOffset resourceBlockStartUtc = CreateTurkeyUtc(effectiveSlotDate, new TimeOnly(10, 30));
        DateTimeOffset resourceBlockEndUtc = CreateTurkeyUtc(effectiveSlotDate, new TimeOnly(11, 15));
        DateTimeOffset availableSlotStartUtc = CreateTurkeyUtc(effectiveSlotDate, new TimeOnly(11, 15));
        tenantContextAccessor.TenantId = tenantId;

        Tenant tenant = Tenant.Create(
            tenantId,
            slug,
            "Atlas Tenant",
            createdAtUtc);
        tenantManagementDbContext.Tenants.Add(tenant);
        await tenantManagementDbContext.SaveChangesAsync();

        Business business = Business.Create(
            tenantId,
            slug,
            "Atlas Hair",
            "hair",
            createdAtUtc,
            "Kadikoy'de modern sac ve bakim salonu.");
        business.UpdatePublicProfile(
            "Randevu saatinden 10 dakika once salonda olunuz.",
            "Atlas Hair Kadikoy",
            "Kadikoy'de sac kesimi ve bakim rezervasyonu.",
            PublicStaffDisplayPolicy.ShowNames);
        business.UpdateRatingSummary(4.8m, 12);
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
        branch.SetPublicSlotSettings(15, 25);
        StaffMember staffMember = StaffMember.Create(
            tenantId,
            branch.Id,
            "Ayse Usta",
            createdAtUtc);
        StaffMember? unqualifiedStaffMember = includeUnqualifiedStaff
            ? StaffMember.Create(
                tenantId,
                branch.Id,
                "Deniz Asistan",
                createdAtUtc)
            : null;
        Skill haircutSkill = Skill.Create(tenantId, "Haircut");
        StaffSkill staffSkill = StaffSkill.Create(tenantId, staffMember.Id, haircutSkill.Id);
        BusinessGalleryImage galleryImage = BusinessGalleryImage.Create(
            tenantId,
            business.Id,
            "https://cdn.example.test/atlas/gallery-1.jpg",
            createdAtUtc,
            "Atlas Hair salon ic mekan",
            sortOrder: 1);

        organizationDbContext.Businesses.Add(business);
        organizationDbContext.Branches.Add(branch);
        organizationDbContext.StaffMembers.Add(staffMember);
        if (unqualifiedStaffMember is not null)
        {
            organizationDbContext.StaffMembers.Add(unqualifiedStaffMember);
        }

        organizationDbContext.Skills.Add(haircutSkill);
        organizationDbContext.StaffSkills.Add(staffSkill);
        organizationDbContext.BusinessGalleryImages.Add(galleryImage);
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
        catalogDbContext.ServiceRequiredSkills.Add(
            ServiceRequiredSkill.Create(
                tenantId,
                variant.Id,
                haircutSkill.Id));
        await catalogDbContext.SaveChangesAsync();

        availabilityDbContext.BranchWorkingHours.Add(
            BranchWorkingHours.Create(
                tenantId,
                branch.Id,
                effectiveSlotDate.DayOfWeek,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0)));
        availabilityDbContext.StaffUnavailableTimes.Add(
            StaffUnavailableTime.Create(
                tenantId,
                staffMember.Id,
                unavailableStartUtc,
                unavailableEndUtc,
                "Break"));
        await availabilityDbContext.SaveChangesAsync();

        Appointment confirmedAppointment = Appointment.CreateConfirmed(
            tenantId,
            null,
            Guid.CreateVersion7(),
            branch.Id,
            staffMember.Id,
            chair.Id,
            confirmedStartUtc,
            confirmedEndUtc,
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
                resourceBlockStartUtc,
                resourceBlockEndUtc,
                "Maintenance"));
        await resourcesDbContext.SaveChangesAsync();

        tenantContextAccessor.TenantId = null;

        return new PublicBusinessProfileSeed(
            tenantId,
            branch.Id,
            slug,
            branchSlug,
            variant.Id,
            haircutSkill.Id,
            staffMember.Id,
            unqualifiedStaffMember?.Id,
            chair.Id,
            availableSlotStartUtc);
    }

    public async Task GrantTenantMembershipAsync(
        Guid tenantId,
        Guid userAccountId,
        TenantMembershipRole role,
        Guid? branchId = null)
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        TenantManagementDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<TenantManagementDbContext>();
        bool tenantExists = await dbContext.Tenants.AnyAsync(entity => entity.Id == tenantId);
        Assert.True(tenantExists);

        dbContext.Memberships.Add(
            TenantMembership.Create(
                tenantId,
                userAccountId,
                role,
                DateTimeOffset.UtcNow,
                branchId));

        await dbContext.SaveChangesAsync();
    }

    public async Task<Guid> GetUserAccountIdAsync(string email)
    {
        using IServiceScope scope = factory!.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await dbContext.Users
            .Where(entity => entity.Email == email)
            .Select(entity => entity.Id)
            .SingleAsync();
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
        TenantManagementDbContext tenantManagementDbContext =
            serviceProvider.GetRequiredService<TenantManagementDbContext>();
        AdminDbContext adminDbContext =
            serviceProvider.GetRequiredService<AdminDbContext>();
        MessagingDbContext messagingDbContext =
            serviceProvider.GetRequiredService<MessagingDbContext>();

        await adminDbContext.Database.MigrateAsync();
        await organizationDbContext.Database.MigrateAsync();
        await catalogDbContext.Database.MigrateAsync();
        await availabilityDbContext.Database.MigrateAsync();
        await resourcesDbContext.Database.MigrateAsync();
        await bookingDbContext.Database.MigrateAsync();
        await messagingDbContext.Database.MigrateAsync();
        await tenantManagementDbContext.Database.MigrateAsync();
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

    private static DateTimeOffset CreateTurkeyUtc(DateOnly date, TimeOnly localTime)
    {
        DateTime localDateTime = date.ToDateTime(localTime, DateTimeKind.Unspecified);
        DateTime utcDateTime = localDateTime.AddHours(-3);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
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
