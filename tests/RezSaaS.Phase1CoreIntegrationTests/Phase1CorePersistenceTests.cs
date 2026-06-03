using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using RezSaaS.BuildingBlocks.Security;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Admin.Infrastructure.Abuse;
using RezSaaS.Modules.Admin.Infrastructure.Auditing;
using RezSaaS.Modules.Admin.Infrastructure.Persistence;
using RezSaaS.Modules.Availability.Application;
using RezSaaS.Modules.Availability.Domain;
using RezSaaS.Modules.Availability.Infrastructure.Persistence;
using RezSaaS.Modules.Booking.Application;
using RezSaaS.Modules.Booking.Domain;
using RezSaaS.Modules.Booking.Infrastructure.Persistence;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;
using RezSaaS.Modules.Messaging.Infrastructure.Queue;
using RezSaaS.Modules.Organization.Application;
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
    public async Task PublicBusinessDirectoryCanResolveActiveBusinessesWithoutTenantContext()
    {
        Guid tenantA = Guid.CreateVersion7();
        Guid tenantB = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantA,
        };

        Business businessA = Business.Create(
            tenantA,
            "atlas-hair",
            "Atlas Hair",
            "hair",
            testTime,
            "Kadıköy'de modern saç ve bakım salonu.");
        Business businessB = Business.Create(
            tenantB,
            "nail-room",
            "Nail Room",
            "nail",
            testTime,
            "Çankaya'da nail studio.");

        await using OrganizationDbContext dbContext =
            new(CreateOptions<OrganizationDbContext>(), tenantContextAccessor);
        dbContext.Businesses.AddRange(businessA, businessB);
        dbContext.Branches.AddRange(
            Branch.Create(
                tenantA,
                businessA.Id,
                "kadikoy",
                "Kadıköy",
                "Europe/Istanbul",
                testTime,
                "İstanbul",
                "Kadıköy",
                "Caferağa Mahallesi"),
            Branch.Create(
                tenantB,
                businessB.Id,
                "cankaya",
                "Çankaya",
                "Europe/Istanbul",
                testTime,
                "Ankara",
                "Çankaya",
                "Kavaklıdere"));
        await dbContext.SaveChangesAsync();

        tenantContextAccessor.TenantId = null;
        PublicBusinessDirectoryService directoryService = new(
            dbContext,
            Options.Create(new PublicBusinessDirectoryOptions()));

        IReadOnlyCollection<PublicBusinessSummaryView> searchResult =
            await directoryService.SearchAsync(
                new PublicBusinessSearchQuery(
                    SearchText: null,
                    CategoryKey: "hair",
                    City: "İstanbul",
                    District: null,
                    Take: null));
        PublicBusinessProfileView? profile =
            await directoryService.GetBySlugAsync("atlas-hair");

        PublicBusinessSummaryView summary = Assert.Single(searchResult);
        Assert.Equal("atlas-hair", summary.Slug);
        Assert.Equal("İstanbul", summary.City);
        Assert.NotNull(profile);
        Assert.Equal("Atlas Hair", profile.DisplayName);
        Assert.Single(profile.Branches);
        Assert.Equal(0, await dbContext.Businesses.CountAsync());
    }

    [Fact]
    public async Task PublicBusinessSlugIsGloballyUniqueAcrossTenants()
    {
        Guid tenantA = Guid.CreateVersion7();
        Guid tenantB = Guid.CreateVersion7();

        await using OrganizationDbContext dbContext =
            new(CreateOptions<OrganizationDbContext>());
        dbContext.Businesses.Add(Business.Create(
            tenantA,
            "atlas-hair",
            "Atlas Hair",
            "hair",
            testTime));
        dbContext.Businesses.Add(Business.Create(
            tenantB,
            "atlas-hair",
            "Atlas Hair Copy",
            "hair",
            testTime));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
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

    [Fact]
    public async Task CreateAppointmentRequestServiceEnforcesUserLimitsAndRecordsAbuse()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        await using BookingDbContext bookingDbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor);
        await using AdminDbContext adminDbContext =
            new(CreateOptions<AdminDbContext>());
        CreateAppointmentRequestService service = new(
            bookingDbContext,
            new AdminAbuseEventRecorder(adminDbContext),
            Options.Create(new BookingSecurityOptions
            {
                DefaultResponseBuffer = TimeSpan.FromHours(2),
                MaxConcurrentPendingRequestsPerUser = 1,
                MaxRequestsPerUserPerDay = 20,
            }),
            tenantContextAccessor,
            new FixedTimeProvider(testTime));

        CreateAppointmentRequestResult firstResult = await service.CreateAsync(
            CreateCommand(customerUserAccountId));
        CreateAppointmentRequestResult secondResult = await service.CreateAsync(
            CreateCommand(customerUserAccountId));

        Assert.True(firstResult.Succeeded);
        Assert.False(secondResult.Succeeded);
        Assert.Equal("BOOKING_PENDING_LIMIT_EXCEEDED", secondResult.ErrorCode);
        Assert.Equal(1, await CountRowsAsync("booking", "AppointmentRequests"));
        Assert.Equal(1, await CountRowsAsync("admin", "AbuseEvents"));
    }

    [Fact]
    public async Task ApproveAppointmentRequestCreatesAppointmentSupersedesConflictsAndQueuesEmail()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid branchId = Guid.CreateVersion7();
        Guid staffMemberId = Guid.CreateVersion7();
        Guid resourceId = Guid.CreateVersion7();
        Guid approverUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };
        AppointmentRequest selectedRequest = CreateRequest(
            tenantId,
            Guid.CreateVersion7(),
            branchId,
            staffMemberId,
            resourceId);
        AppointmentRequest conflictingRequest = CreateRequest(
            tenantId,
            Guid.CreateVersion7(),
            branchId,
            staffMemberId,
            resourceId);

        await using BookingDbContext bookingDbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor);
        bookingDbContext.AppointmentRequests.AddRange(selectedRequest, conflictingRequest);
        await bookingDbContext.SaveChangesAsync();
        await using AdminDbContext adminDbContext =
            new(CreateOptions<AdminDbContext>());
        await using MessagingDbContext messagingDbContext =
            new(CreateOptions<MessagingDbContext>(), tenantContextAccessor);
        ApproveAppointmentRequestService service = new(
            bookingDbContext,
            new AdminAuditLogRecorder(adminDbContext),
            new TransactionalMessageOutbox(messagingDbContext),
            tenantContextAccessor,
            new FixedTimeProvider(testTime));

        AppointmentRequestDecisionResult result = await service.ApproveAsync(
            selectedRequest.Id,
            approverUserAccountId);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.AffectedRequests);
        Assert.NotNull(result.AppointmentId);
        Assert.Equal(AppointmentRequestStatus.Approved, selectedRequest.Status);
        Assert.Equal(AppointmentRequestStatus.Superseded, conflictingRequest.Status);
        Assert.Equal(1, await CountRowsAsync("booking", "Appointments"));
        Assert.Equal(1, await CountRowsAsync("messaging", "TransactionalMessages"));
        Assert.Equal(1, await CountRowsAsync("admin", "AdminAuditLogEntries"));
    }

    [Fact]
    public async Task ExpireAppointmentRequestsServiceClosesDuePendingRequests()
    {
        Guid tenantId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };
        AppointmentRequest request = AppointmentRequest.Create(
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            testTime.AddHours(6),
            testTime.AddHours(7),
            testTime.AddDays(-2),
            TimeSpan.FromHours(2));
        request.AddLine(Guid.CreateVersion7(), "Haircut", 60, 500, "TRY");

        await using BookingDbContext dbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor);
        dbContext.AppointmentRequests.Add(request);
        await dbContext.SaveChangesAsync();
        ExpireAppointmentRequestsService service = new(
            dbContext,
            tenantContextAccessor,
            new FixedTimeProvider(testTime));

        int expiredCount = await service.ExpireDueAsync();

        Assert.Equal(1, expiredCount);
        Assert.Equal(AppointmentRequestStatus.Expired, request.Status);
    }

    [Fact]
    public async Task AvailabilityQueryServiceReturnsTenantScopedSnapshot()
    {
        Guid tenantA = Guid.CreateVersion7();
        Guid tenantB = Guid.CreateVersion7();
        Guid branchId = Guid.CreateVersion7();
        Guid staffMemberId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantA,
        };

        await using AvailabilityDbContext dbContext =
            new(CreateOptions<AvailabilityDbContext>(), tenantContextAccessor);
        dbContext.BranchWorkingHours.Add(
            BranchWorkingHours.Create(
                tenantA,
                branchId,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(18, 0)));
        dbContext.StaffUnavailableTimes.Add(
            StaffUnavailableTime.Create(
                tenantA,
                staffMemberId,
                testTime.AddHours(2),
                testTime.AddHours(3),
                "Leave"));
        dbContext.StaffUnavailableTimes.Add(
            StaffUnavailableTime.Create(
                tenantB,
                Guid.CreateVersion7(),
                testTime.AddHours(2),
                testTime.AddHours(3),
                "Other tenant"));
        await dbContext.SaveChangesAsync();
        AvailabilityQueryService service = new(dbContext, tenantContextAccessor);

        AvailabilitySnapshot? snapshot = await service.GetBranchSnapshotAsync(
            branchId,
            testTime,
            testTime.AddDays(7),
            [staffMemberId]);

        Assert.NotNull(snapshot);
        Assert.Single(snapshot.WorkingHours);
        Assert.Single(snapshot.StaffUnavailableTimes);

        tenantContextAccessor.TenantId = tenantB;
        AvailabilitySnapshot? hiddenSnapshot = await service.GetBranchSnapshotAsync(
            branchId,
            testTime,
            testTime.AddDays(7),
            [staffMemberId]);

        Assert.NotNull(hiddenSnapshot);
        Assert.Empty(hiddenSnapshot.WorkingHours);
        Assert.Empty(hiddenSnapshot.StaffUnavailableTimes);
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

    private CreateAppointmentRequestCommand CreateCommand(Guid customerUserAccountId)
    {
        return new CreateAppointmentRequestCommand(
            customerUserAccountId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            testTime.AddDays(1),
            testTime.AddDays(1).AddHours(1),
            [
                new AppointmentRequestLineInput(
                    Guid.CreateVersion7(),
                    "Haircut",
                    60,
                    500,
                    "TRY"),
            ]);
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

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
