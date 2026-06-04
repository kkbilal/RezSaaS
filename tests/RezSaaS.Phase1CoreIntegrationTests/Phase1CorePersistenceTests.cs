using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using RezSaaS.BuildingBlocks.Security;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Admin.Domain;
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
        Assert.Equal(0, await CountRowsAsync("admin", "AbuseAppeals"));
        Assert.Equal(0, await CountRowsAsync("admin", "AccountClosureCases"));
        Assert.Equal(0, await CountRowsAsync("admin", "BusinessAbuseReports"));
        Assert.Equal(0, await CountRowsAsync("admin", "UserStrikes"));
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
            new AdminUserBookingRestrictionEvaluator(adminDbContext),
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
    public async Task UserSanctionServiceAuditsAndBlocksNewBookingRequests()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        Guid customerUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };
        await using AdminDbContext adminDbContext =
            new(CreateOptions<AdminDbContext>());
        ApplyUserSanctionService sanctionService = new(
            adminDbContext,
            new FixedTimeProvider(testTime));
        RevokeUserSanctionService revokeSanctionService = new(
            adminDbContext,
            new FixedTimeProvider(testTime.AddMinutes(5)));
        AbuseReportQueryService abuseReportQueryService = new(
            adminDbContext,
            Options.Create(new AbuseRiskOptions()),
            new FixedTimeProvider(testTime));
        AbuseControlPlaneQueryService queryService = new(
            adminDbContext,
            abuseReportQueryService,
            new FixedTimeProvider(testTime));
        AdminUserBookingRestrictionEvaluator restrictionEvaluator = new(adminDbContext);

        ApplyUserSanctionResult warningResult =
            await sanctionService.ApplyAsync(
                new ApplyUserSanctionCommand(
                    actorUserAccountId,
                    customerUserAccountId,
                    UserSanctionType.Warning,
                    "First abuse warning",
                    EndsAtUtc: null));
        ApplyUserSanctionResult cooldownResult =
            await sanctionService.ApplyAsync(
                new ApplyUserSanctionCommand(
                    actorUserAccountId,
                    customerUserAccountId,
                    UserSanctionType.Cooldown,
                    "Repeated slot spam",
                    testTime.AddHours(2)));
        ApplyUserSanctionResult duplicateBlockingResult =
            await sanctionService.ApplyAsync(
                new ApplyUserSanctionCommand(
                    actorUserAccountId,
                    customerUserAccountId,
                    UserSanctionType.TemporaryBan,
                    "Escalated slot spam",
                    testTime.AddHours(48)));
        ApplyUserSanctionResult permanentClosureResult =
            await sanctionService.ApplyAsync(
                new ApplyUserSanctionCommand(
                    actorUserAccountId,
                    customerUserAccountId,
                    UserSanctionType.PermanentClosure,
                    "Manual closure request",
                    EndsAtUtc: null));

        Assert.True(warningResult.Succeeded);
        Assert.True(cooldownResult.Succeeded);
        Assert.False(duplicateBlockingResult.Succeeded);
        Assert.Equal("USER_ACTIVE_SANCTION_EXISTS", duplicateBlockingResult.ErrorCode);
        Assert.False(permanentClosureResult.Succeeded);
        Assert.Equal(
            "USER_PERMANENT_CLOSURE_REQUIRES_ACCOUNT_WORKFLOW",
            permanentClosureResult.ErrorCode);

        UserAbuseOverviewView overview =
            (await queryService.GetUserOverviewAsync(customerUserAccountId))!;
        Assert.Equal(2, overview.Sanctions.Count);
        Assert.Single(overview.Sanctions, entity => entity.IsActive);
        Assert.Equal(
            "Cooldown",
            (await restrictionEvaluator.EvaluateAsync(customerUserAccountId, testTime))
            .RestrictionCode);

        await using BookingDbContext bookingDbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor);
        CreateAppointmentRequestService bookingService = new(
            bookingDbContext,
            new AdminAbuseEventRecorder(adminDbContext),
            restrictionEvaluator,
            Options.Create(new BookingSecurityOptions
            {
                DefaultResponseBuffer = TimeSpan.FromHours(2),
                MaxConcurrentPendingRequestsPerUser = 5,
                MaxRequestsPerUserPerDay = 20,
            }),
            tenantContextAccessor,
            new FixedTimeProvider(testTime));

        CreateAppointmentRequestResult bookingResult =
            await bookingService.CreateAsync(CreateCommand(customerUserAccountId));

        Assert.False(bookingResult.Succeeded);
        Assert.Equal("BOOKING_USER_SANCTIONED", bookingResult.ErrorCode);
        ApplyUserSanctionResult warningRevokeResult =
            await revokeSanctionService.RevokeAsync(
                new RevokeUserSanctionCommand(
                    actorUserAccountId,
                    customerUserAccountId,
                    warningResult.SanctionId!.Value,
                    "Warnings remain historical"));
        ApplyUserSanctionResult revokeResult =
            await revokeSanctionService.RevokeAsync(
                new RevokeUserSanctionCommand(
                    actorUserAccountId,
                    customerUserAccountId,
                    cooldownResult.SanctionId!.Value,
                    "Manual review cleared the restriction"));
        ApplyUserSanctionResult duplicateRevokeResult =
            await revokeSanctionService.RevokeAsync(
                new RevokeUserSanctionCommand(
                    actorUserAccountId,
                    customerUserAccountId,
                    cooldownResult.SanctionId.Value,
                    "Retry"));
        CreateAppointmentRequestResult bookingAfterRevokeResult =
            await bookingService.CreateAsync(CreateCommand(customerUserAccountId));

        Assert.False(warningRevokeResult.Succeeded);
        Assert.Equal("USER_SANCTION_NOT_REVOCABLE", warningRevokeResult.ErrorCode);
        Assert.True(revokeResult.Succeeded);
        Assert.True(duplicateRevokeResult.Succeeded);
        Assert.True(bookingAfterRevokeResult.Succeeded);
        Assert.False(
            (await restrictionEvaluator.EvaluateAsync(customerUserAccountId, testTime.AddMinutes(5)))
            .IsRestricted);
        UserAbuseOverviewView overviewAfterRevoke =
            (await queryService.GetUserOverviewAsync(customerUserAccountId))!;
        UserSanctionView revokedSanction = overviewAfterRevoke.Sanctions
            .Single(entity => entity.Id == cooldownResult.SanctionId);
        Assert.False(revokedSanction.IsActive);
        Assert.Equal(testTime.AddMinutes(5), revokedSanction.RevokedAtUtc);
        Assert.Equal(actorUserAccountId, revokedSanction.RevokedByUserAccountId);
        Assert.Equal(1, await CountRowsAsync("booking", "AppointmentRequests"));
        Assert.Equal(2, await CountRowsAsync("admin", "UserSanctions"));
        Assert.Equal(3, await CountRowsAsync("admin", "AdminAuditLogEntries"));
    }

    [Fact]
    public void UserSanctionDomainEnforcesTemporalShapeAndSafeRevocation()
    {
        Guid userAccountId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();

        Assert.Throws<ArgumentException>(() =>
            UserSanction.Create(
                userAccountId,
                UserSanctionType.Cooldown,
                "Cooldown requires end",
                testTime,
                endsAtUtc: null));
        Assert.Throws<ArgumentException>(() =>
            UserSanction.Create(
                userAccountId,
                UserSanctionType.Warning,
                "Warning cannot have end",
                testTime,
                testTime.AddHours(1)));

        UserSanction sanction = UserSanction.Create(
            userAccountId,
            UserSanctionType.Cooldown,
            "Cooldown",
            testTime,
            testTime.AddHours(1));

        Assert.Throws<ArgumentException>(() =>
            sanction.Revoke(
                actorUserAccountId,
                string.Empty,
                testTime.AddMinutes(1)));
        Assert.Null(sanction.RevokedAtUtc);
        Assert.Null(sanction.RevokedByUserAccountId);
        Assert.Null(sanction.RevocationReason);
    }

    [Fact]
    public async Task BusinessAbuseReportRequiresAdminReviewBeforeCreatingRevocableStrike()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid branchId = Guid.CreateVersion7();
        Guid appointmentRequestId = Guid.CreateVersion7();
        Guid secondAppointmentRequestId = Guid.CreateVersion7();
        Guid customerUserAccountId = Guid.CreateVersion7();
        Guid businessActorUserAccountId = Guid.CreateVersion7();
        Guid adminUserAccountId = Guid.CreateVersion7();
        AbuseRiskOptions riskOptions = new()
        {
            ElevatedStrikeThreshold = 2,
            HighStrikeThreshold = 3,
            MaxBusinessReportsPerActorPerDay = 1,
            StrikeLifetimeDays = 90,
        };
        await using AdminDbContext dbContext = new(CreateOptions<AdminDbContext>());
        CreateBusinessAbuseReportService createService = new(
            dbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime));
        ReviewBusinessAbuseReportService reviewService = new(
            dbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddHours(1)));
        RevokeUserStrikeService revokeStrikeService = new(
            dbContext,
            new FixedTimeProvider(testTime.AddHours(2)));
        AbuseReportQueryService activeQueryService = new(
            dbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddHours(1)));

        CreateBusinessAbuseReportCommand reportCommand = new(
            tenantId,
            branchId,
            appointmentRequestId,
            customerUserAccountId,
            businessActorUserAccountId,
            AbuseReportReasonCode.SlotSpam,
            "Repeated overlapping requests");
        BusinessAbuseReportCommandResult createResult =
            await createService.CreateAsync(reportCommand);
        BusinessAbuseReportCommandResult replayResult =
            await createService.CreateAsync(reportCommand);
        BusinessAbuseReportCommandResult dailyLimitResult =
            await createService.CreateAsync(
                reportCommand with { AppointmentRequestId = secondAppointmentRequestId });

        Assert.True(createResult.Succeeded);
        Assert.True(createResult.Created);
        Assert.True(replayResult.Succeeded);
        Assert.False(replayResult.Created);
        Assert.Equal(createResult.ReportId, replayResult.ReportId);
        Assert.False(dailyLimitResult.Succeeded);
        Assert.Equal("BUSINESS_ABUSE_REPORT_DAILY_LIMIT_EXCEEDED", dailyLimitResult.ErrorCode);
        Assert.Equal(1, await CountRowsAsync("admin", "BusinessAbuseReports"));
        Assert.Equal(0, await CountRowsAsync("admin", "UserStrikes"));

        ReviewBusinessAbuseReportResult confirmResult =
            await reviewService.ReviewAsync(
                new ReviewBusinessAbuseReportCommand(
                    adminUserAccountId,
                    createResult.ReportId!.Value,
                    AbuseReportStatus.Confirmed,
                    "Evidence verified"));
        ReviewBusinessAbuseReportResult confirmReplayResult =
            await reviewService.ReviewAsync(
                new ReviewBusinessAbuseReportCommand(
                    adminUserAccountId,
                    createResult.ReportId.Value,
                    AbuseReportStatus.Confirmed,
                    "Evidence verified"));
        ReviewBusinessAbuseReportResult conflictingDismissResult =
            await reviewService.ReviewAsync(
                new ReviewBusinessAbuseReportCommand(
                    adminUserAccountId,
                    createResult.ReportId.Value,
                    AbuseReportStatus.Dismissed,
                    "Conflicting decision"));
        UserRiskSummaryView activeRisk =
            await activeQueryService.GetUserRiskSummaryAsync(customerUserAccountId);

        Assert.True(confirmResult.Succeeded);
        Assert.NotNull(confirmResult.StrikeId);
        Assert.Equal(confirmResult.StrikeId, confirmReplayResult.StrikeId);
        Assert.False(conflictingDismissResult.Succeeded);
        Assert.Equal("BUSINESS_ABUSE_REPORT_ALREADY_REVIEWED", conflictingDismissResult.ErrorCode);
        Assert.Equal(1, activeRisk.ActiveStrikeCount);
        Assert.Equal(UserRiskLevel.Monitor, activeRisk.Level);

        UserStrikeCommandResult revokeResult =
            await revokeStrikeService.RevokeAsync(
                new RevokeUserStrikeCommand(
                    adminUserAccountId,
                    customerUserAccountId,
                    confirmResult.StrikeId!.Value,
                    "Review correction"));
        UserStrikeCommandResult revokeReplayResult =
            await revokeStrikeService.RevokeAsync(
                new RevokeUserStrikeCommand(
                    adminUserAccountId,
                    customerUserAccountId,
                    confirmResult.StrikeId.Value,
                    "Review correction retry"));
        AbuseReportQueryService revokedQueryService = new(
            dbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddHours(2)));
        UserRiskSummaryView revokedRisk =
            await revokedQueryService.GetUserRiskSummaryAsync(customerUserAccountId);
        UserStrikeView strike =
            (await revokedQueryService.GetUserStrikesAsync(customerUserAccountId)).Single();

        Assert.True(revokeResult.Succeeded);
        Assert.True(revokeReplayResult.Succeeded);
        Assert.Equal(0, revokedRisk.ActiveStrikeCount);
        Assert.Equal(UserRiskLevel.Normal, revokedRisk.Level);
        Assert.False(strike.IsActive);
        Assert.Equal(adminUserAccountId, strike.RevokedByUserAccountId);
        Assert.Equal(3, await CountRowsAsync("admin", "AbuseEvents"));
        Assert.Equal(1, await CountRowsAsync("admin", "UserStrikes"));
        Assert.Equal(3, await CountRowsAsync("admin", "AdminAuditLogEntries"));
    }

    [Fact]
    public void BusinessAbuseReportDomainRejectsUnsafeSelfReportAndPartialReview()
    {
        Guid userAccountId = Guid.CreateVersion7();

        Assert.Throws<ArgumentException>(() =>
            BusinessAbuseReport.Create(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                userAccountId,
                userAccountId,
                AbuseReportReasonCode.Other,
                note: null,
                createdAtUtc: testTime));

        BusinessAbuseReport report = BusinessAbuseReport.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            userAccountId,
            Guid.CreateVersion7(),
            AbuseReportReasonCode.SlotSpam,
            note: null,
            createdAtUtc: testTime);

        Assert.Throws<ArgumentException>(() =>
            report.Review(
                AbuseReportStatus.Confirmed,
                Guid.CreateVersion7(),
                string.Empty,
                testTime.AddMinutes(1)));
        Assert.Equal(AbuseReportStatus.PendingReview, report.Status);
        Assert.Null(report.ReviewedAtUtc);
        Assert.Null(report.ReviewedByUserAccountId);
    }

    [Fact]
    public async Task BusinessAbuseReportAndConfirmationRetriesAreConcurrencySafe()
    {
        AbuseRiskOptions riskOptions = new();
        CreateBusinessAbuseReportCommand command = new(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            AbuseReportReasonCode.SuspectedAutomation,
            Note: null);
        await using AdminDbContext firstCreateDbContext =
            new(CreateOptions<AdminDbContext>());
        await using AdminDbContext secondCreateDbContext =
            new(CreateOptions<AdminDbContext>());
        CreateBusinessAbuseReportService firstCreateService = new(
            firstCreateDbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime));
        CreateBusinessAbuseReportService secondCreateService = new(
            secondCreateDbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime));

        BusinessAbuseReportCommandResult[] createResults =
            await Task.WhenAll(
                firstCreateService.CreateAsync(command),
                secondCreateService.CreateAsync(command));

        Assert.All(createResults, result => Assert.True(result.Succeeded));
        Assert.Single(createResults.Select(result => result.ReportId).Distinct());
        Assert.Equal(1, createResults.Count(result => result.Created));
        Assert.Equal(1, await CountRowsAsync("admin", "BusinessAbuseReports"));

        Guid reportId = createResults[0].ReportId!.Value;
        Guid adminUserAccountId = Guid.CreateVersion7();
        ReviewBusinessAbuseReportCommand reviewCommand = new(
            adminUserAccountId,
            reportId,
            AbuseReportStatus.Confirmed,
            "Concurrent evidence verification");
        await using AdminDbContext firstReviewDbContext =
            new(CreateOptions<AdminDbContext>());
        await using AdminDbContext secondReviewDbContext =
            new(CreateOptions<AdminDbContext>());
        ReviewBusinessAbuseReportService firstReviewService = new(
            firstReviewDbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddHours(1)));
        ReviewBusinessAbuseReportService secondReviewService = new(
            secondReviewDbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddHours(1)));

        ReviewBusinessAbuseReportResult[] reviewResults =
            await Task.WhenAll(
                firstReviewService.ReviewAsync(reviewCommand),
                secondReviewService.ReviewAsync(reviewCommand));

        Assert.All(reviewResults, result => Assert.True(result.Succeeded));
        Assert.Single(reviewResults.Select(result => result.StrikeId).Distinct());
        Assert.Equal(1, await CountRowsAsync("admin", "UserStrikes"));
    }

    [Fact]
    public async Task AccountClosureWaitsForAppealReviewAndCreatesPermanentSanction()
    {
        Guid customerUserAccountId = Guid.CreateVersion7();
        Guid proposingAdminUserAccountId = Guid.CreateVersion7();
        Guid reviewingAdminUserAccountId = Guid.CreateVersion7();
        Guid executingAdminUserAccountId = Guid.CreateVersion7();
        AbuseRiskOptions riskOptions = new()
        {
            AccountClosureExecutionEnabled = true,
            ClosureAppealWindowDays = 7,
            ElevatedStrikeThreshold = 2,
            HighStrikeThreshold = 3,
            MaxOpenAppealsPerUser = 3,
        };
        await using AdminDbContext dbContext = new(CreateOptions<AdminDbContext>());

        for (int index = 0; index < riskOptions.HighStrikeThreshold; index++)
        {
            BusinessAbuseReport report = BusinessAbuseReport.Create(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                customerUserAccountId,
                Guid.CreateVersion7(),
                AbuseReportReasonCode.SlotSpam,
                note: null,
                testTime);
            report.Review(
                AbuseReportStatus.Confirmed,
                reviewingAdminUserAccountId,
                "Evidence verified.",
                testTime.AddMinutes(1));
            dbContext.BusinessAbuseReports.Add(report);
            dbContext.UserStrikes.Add(
                UserStrike.Create(
                    customerUserAccountId,
                    report.TenantId,
                    report.Id,
                    report.ReasonCode,
                    reviewingAdminUserAccountId,
                    testTime.AddMinutes(1),
                    testTime.AddDays(90)));
        }

        await dbContext.SaveChangesAsync();
        ProposeAccountClosureService proposeService = new(
            dbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddHours(1)));
        ReviewAccountClosureService reviewClosureService = new(
            dbContext,
            new FixedTimeProvider(testTime.AddHours(2)));
        AbuseWorkflowCommandResult proposalResult =
            await proposeService.ProposeAsync(
                new ProposeAccountClosureCommand(
                    proposingAdminUserAccountId,
                    customerUserAccountId,
                    "Verified high-risk evidence.",
                    "Your account is scheduled for closure after an appeal window."));
        Assert.True(proposalResult.Succeeded);

        AbuseWorkflowCommandResult selfApprovalResult =
            await reviewClosureService.ReviewAsync(
                new ReviewAccountClosureCommand(
                    proposingAdminUserAccountId,
                    proposalResult.EntityId!.Value,
                    AccountClosureCaseStatus.Approved,
                    "Self approval."));
        AbuseWorkflowCommandResult approvalResult =
            await reviewClosureService.ReviewAsync(
                new ReviewAccountClosureCommand(
                    reviewingAdminUserAccountId,
                    proposalResult.EntityId.Value,
                    AccountClosureCaseStatus.Approved,
                    "Independent evidence review."));
        AbuseWorkflowCommandResult selfApprovalReplayResult =
            await reviewClosureService.ReviewAsync(
                new ReviewAccountClosureCommand(
                    proposingAdminUserAccountId,
                    proposalResult.EntityId.Value,
                    AccountClosureCaseStatus.Approved,
                    "Self approval replay."));

        Assert.False(selfApprovalResult.Succeeded);
        Assert.Equal("ACCOUNT_CLOSURE_REQUIRES_SECOND_ADMIN", selfApprovalResult.ErrorCode);
        Assert.True(approvalResult.Succeeded);
        Assert.False(selfApprovalReplayResult.Succeeded);
        Assert.Equal(
            "ACCOUNT_CLOSURE_REQUIRES_SECOND_ADMIN",
            selfApprovalReplayResult.ErrorCode);

        CreateAbuseAppealService createAppealService = new(
            dbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddHours(3)));
        AbuseWorkflowCommandResult appealResult =
            await createAppealService.CreateAsync(
                new CreateAbuseAppealCommand(
                    customerUserAccountId,
                    AbuseAppealTargetType.AccountClosureCase,
                    proposalResult.EntityId.Value,
                    "The evidence is attributed to the wrong account."));
        Assert.True(appealResult.Succeeded);

        AccountClosureExecutionService executionService = new(
            dbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddDays(8)));
        ExecuteAccountClosureCommand executionCommand =
            new(executingAdminUserAccountId, proposalResult.EntityId.Value);
        AbuseWorkflowCommandResult blockedExecutionResult =
            await executionService.BeginAsync(executionCommand);

        Assert.False(blockedExecutionResult.Succeeded);
        Assert.Equal("ACCOUNT_CLOSURE_APPEAL_PENDING", blockedExecutionResult.ErrorCode);

        ReviewAbuseAppealService reviewAppealService = new(
            dbContext,
            new FixedTimeProvider(testTime.AddDays(8)));
        AbuseWorkflowCommandResult rejectedAppealResult =
            await reviewAppealService.ReviewAsync(
                new ReviewAbuseAppealCommand(
                    reviewingAdminUserAccountId,
                    appealResult.EntityId!.Value,
                    AbuseAppealStatus.Rejected,
                    "Evidence attribution was reconfirmed."));
        AbuseWorkflowCommandResult selfAppealReplayResult =
            await reviewAppealService.ReviewAsync(
                new ReviewAbuseAppealCommand(
                    customerUserAccountId,
                    appealResult.EntityId.Value,
                    AbuseAppealStatus.Rejected,
                    "Self review replay."));
        UserStrike revokedStrike = await dbContext.UserStrikes.FirstAsync();
        revokedStrike.Revoke(
            executingAdminUserAccountId,
            "Evidence was independently corrected.",
            testTime.AddDays(8));
        await dbContext.SaveChangesAsync();
        AbuseWorkflowCommandResult riskBlockedExecutionResult =
            await executionService.BeginAsync(executionCommand);
        BusinessAbuseReport replacementReport = BusinessAbuseReport.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            customerUserAccountId,
            Guid.CreateVersion7(),
            AbuseReportReasonCode.SuspectedAutomation,
            note: null,
            testTime.AddDays(7));
        replacementReport.Review(
            AbuseReportStatus.Confirmed,
            reviewingAdminUserAccountId,
            "Replacement evidence verified.",
            testTime.AddDays(7).AddMinutes(1));
        dbContext.BusinessAbuseReports.Add(replacementReport);
        dbContext.UserStrikes.Add(
            UserStrike.Create(
                customerUserAccountId,
                replacementReport.TenantId,
                replacementReport.Id,
                replacementReport.ReasonCode,
                reviewingAdminUserAccountId,
                testTime.AddDays(7).AddMinutes(1),
                testTime.AddDays(97)));
        await dbContext.SaveChangesAsync();
        AbuseWorkflowCommandResult beginResult =
            await executionService.BeginAsync(executionCommand);
        AbuseWorkflowCommandResult completeResult =
            await executionService.CompleteAsync(executionCommand);

        Assert.True(rejectedAppealResult.Succeeded);
        Assert.False(selfAppealReplayResult.Succeeded);
        Assert.Equal("ABUSE_APPEAL_SELF_REVIEW_FORBIDDEN", selfAppealReplayResult.ErrorCode);
        Assert.False(riskBlockedExecutionResult.Succeeded);
        Assert.Equal(
            "ACCOUNT_CLOSURE_RISK_NO_LONGER_HIGH",
            riskBlockedExecutionResult.ErrorCode);
        Assert.True(beginResult.Succeeded);
        Assert.True(completeResult.Succeeded);
        Assert.Equal(
            AccountClosureCaseStatus.Executed,
            await dbContext.AccountClosureCases
                .Where(entity => entity.Id == proposalResult.EntityId)
                .Select(entity => entity.Status)
                .SingleAsync());
        Assert.Equal(
            1,
            await dbContext.UserSanctions.CountAsync(
                entity => entity.UserAccountId == customerUserAccountId
                    && entity.Type == UserSanctionType.PermanentClosure));
    }

    [Fact]
    public async Task AbuseWorkflowSerializesStrikeRevocationAndClosureExecution()
    {
        Guid customerUserAccountId = Guid.CreateVersion7();
        Guid proposingAdminUserAccountId = Guid.CreateVersion7();
        Guid reviewingAdminUserAccountId = Guid.CreateVersion7();
        Guid executingAdminUserAccountId = Guid.CreateVersion7();
        AbuseRiskOptions riskOptions = new()
        {
            AccountClosureExecutionEnabled = true,
            ClosureAppealWindowDays = 7,
            HighStrikeThreshold = 3,
        };
        List<UserStrike> strikes = [];
        AccountClosureCase closureCase = AccountClosureCase.Create(
            customerUserAccountId,
            proposingAdminUserAccountId,
            "Verified high-risk evidence.",
            "Your account is scheduled for closure after an appeal window.",
            testTime,
            testTime.AddDays(riskOptions.ClosureAppealWindowDays));
        closureCase.Approve(
            reviewingAdminUserAccountId,
            "Independent evidence review.",
            testTime.AddHours(1));

        await using (AdminDbContext seedDbContext = new(CreateOptions<AdminDbContext>()))
        {
            for (int index = 0; index < riskOptions.HighStrikeThreshold; index++)
            {
                BusinessAbuseReport report = BusinessAbuseReport.Create(
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    customerUserAccountId,
                    Guid.CreateVersion7(),
                    AbuseReportReasonCode.SlotSpam,
                    note: null,
                    testTime);
                report.Review(
                    AbuseReportStatus.Confirmed,
                    reviewingAdminUserAccountId,
                    "Evidence verified.",
                    testTime.AddMinutes(1));
                UserStrike strike = UserStrike.Create(
                    customerUserAccountId,
                    report.TenantId,
                    report.Id,
                    report.ReasonCode,
                    reviewingAdminUserAccountId,
                    testTime.AddMinutes(1),
                    testTime.AddDays(90));
                strikes.Add(strike);
                seedDbContext.BusinessAbuseReports.Add(report);
                seedDbContext.UserStrikes.Add(strike);
            }

            seedDbContext.AccountClosureCases.Add(closureCase);
            await seedDbContext.SaveChangesAsync();
        }

        await using NpgsqlConnection lockConnection = new(DatabaseConnectionString);
        await lockConnection.OpenAsync();
        await using NpgsqlTransaction revocationLockTransaction =
            await lockConnection.BeginTransactionAsync();
        await AcquireAbuseUserWorkflowLockAsync(
            lockConnection,
            revocationLockTransaction,
            customerUserAccountId);

        await using AdminDbContext revocationDbContext = new(CreateOptions<AdminDbContext>());
        RevokeUserStrikeService revocationService = new(
            revocationDbContext,
            new FixedTimeProvider(testTime.AddDays(8)));
        Task<UserStrikeCommandResult> revocationTask = revocationService.RevokeAsync(
            new RevokeUserStrikeCommand(
                reviewingAdminUserAccountId,
                customerUserAccountId,
                strikes[0].Id,
                "Evidence correction."));

        Task firstCompletedTask = await Task.WhenAny(
            revocationTask,
            Task.Delay(TimeSpan.FromMilliseconds(300)));
        Assert.NotSame(revocationTask, firstCompletedTask);

        await revocationLockTransaction.CommitAsync();
        UserStrikeCommandResult revocationResult =
            await revocationTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(revocationResult.Succeeded);

        ExecuteAccountClosureCommand executionCommand =
            new(executingAdminUserAccountId, closureCase.Id);
        await using AdminDbContext riskCheckDbContext = new(CreateOptions<AdminDbContext>());
        AccountClosureExecutionService riskCheckService = new(
            riskCheckDbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddDays(8)));
        AbuseWorkflowCommandResult riskBlockedResult =
            await riskCheckService.BeginAsync(executionCommand);

        Assert.False(riskBlockedResult.Succeeded);
        Assert.Equal("ACCOUNT_CLOSURE_RISK_NO_LONGER_HIGH", riskBlockedResult.ErrorCode);

        await using (AdminDbContext replacementDbContext = new(CreateOptions<AdminDbContext>()))
        {
            BusinessAbuseReport replacementReport = BusinessAbuseReport.Create(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                customerUserAccountId,
                Guid.CreateVersion7(),
                AbuseReportReasonCode.SuspectedAutomation,
                note: null,
                testTime.AddDays(7));
            replacementReport.Review(
                AbuseReportStatus.Confirmed,
                reviewingAdminUserAccountId,
                "Replacement evidence verified.",
                testTime.AddDays(7).AddMinutes(1));
            replacementDbContext.BusinessAbuseReports.Add(replacementReport);
            replacementDbContext.UserStrikes.Add(
                UserStrike.Create(
                    customerUserAccountId,
                    replacementReport.TenantId,
                    replacementReport.Id,
                    replacementReport.ReasonCode,
                    reviewingAdminUserAccountId,
                    testTime.AddDays(7).AddMinutes(1),
                    testTime.AddDays(97)));
            await replacementDbContext.SaveChangesAsync();
        }

        await using NpgsqlTransaction executionLockTransaction =
            await lockConnection.BeginTransactionAsync();
        await AcquireAbuseUserWorkflowLockAsync(
            lockConnection,
            executionLockTransaction,
            customerUserAccountId);
        await using AdminDbContext executionDbContext = new(CreateOptions<AdminDbContext>());
        AccountClosureExecutionService executionService = new(
            executionDbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddDays(8)));
        Task<AbuseWorkflowCommandResult> executionTask =
            executionService.BeginAsync(executionCommand);

        firstCompletedTask = await Task.WhenAny(
            executionTask,
            Task.Delay(TimeSpan.FromMilliseconds(300)));
        Assert.NotSame(executionTask, firstCompletedTask);

        await executionLockTransaction.CommitAsync();
        AbuseWorkflowCommandResult executionResult =
            await executionTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(executionResult.Succeeded);
    }

    [Fact]
    public async Task AbuseWorkflowSerializesSanctionApplicationAndClosureCompletion()
    {
        Guid customerUserAccountId = Guid.CreateVersion7();
        Guid proposingAdminUserAccountId = Guid.CreateVersion7();
        Guid reviewingAdminUserAccountId = Guid.CreateVersion7();
        Guid executingAdminUserAccountId = Guid.CreateVersion7();
        DateTimeOffset executionTime = testTime.AddDays(8);
        AccountClosureCase closureCase = AccountClosureCase.Create(
            customerUserAccountId,
            proposingAdminUserAccountId,
            "Verified high-risk evidence.",
            "Your account is scheduled for closure after an appeal window.",
            testTime,
            testTime.AddDays(7));
        closureCase.Approve(
            reviewingAdminUserAccountId,
            "Independent evidence review.",
            testTime.AddHours(1));
        closureCase.BeginExecution(executingAdminUserAccountId, executionTime);

        await using (AdminDbContext seedDbContext = new(CreateOptions<AdminDbContext>()))
        {
            seedDbContext.AccountClosureCases.Add(closureCase);
            await seedDbContext.SaveChangesAsync();
        }

        await using NpgsqlConnection lockConnection = new(DatabaseConnectionString);
        await lockConnection.OpenAsync();
        await using NpgsqlTransaction sanctionLockTransaction =
            await lockConnection.BeginTransactionAsync();
        await AcquireAbuseUserWorkflowLockAsync(
            lockConnection,
            sanctionLockTransaction,
            customerUserAccountId);
        await using AdminDbContext sanctionDbContext = new(CreateOptions<AdminDbContext>());
        ApplyUserSanctionService sanctionService = new(
            sanctionDbContext,
            new FixedTimeProvider(executionTime));
        Task<ApplyUserSanctionResult> sanctionTask = sanctionService.ApplyAsync(
            new ApplyUserSanctionCommand(
                reviewingAdminUserAccountId,
                customerUserAccountId,
                UserSanctionType.Cooldown,
                "Manual review cooldown.",
                executionTime.AddHours(1)));

        Task firstCompletedTask = await Task.WhenAny(
            sanctionTask,
            Task.Delay(TimeSpan.FromMilliseconds(300)));
        Assert.NotSame(sanctionTask, firstCompletedTask);

        await sanctionLockTransaction.CommitAsync();
        ApplyUserSanctionResult sanctionResult =
            await sanctionTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(sanctionResult.Succeeded);

        await using NpgsqlTransaction completionLockTransaction =
            await lockConnection.BeginTransactionAsync();
        await AcquireAbuseUserWorkflowLockAsync(
            lockConnection,
            completionLockTransaction,
            customerUserAccountId);
        await using AdminDbContext completionDbContext = new(CreateOptions<AdminDbContext>());
        AccountClosureExecutionService completionService = new(
            completionDbContext,
            Options.Create(new AbuseRiskOptions()),
            new FixedTimeProvider(executionTime));
        ExecuteAccountClosureCommand executionCommand =
            new(executingAdminUserAccountId, closureCase.Id);
        Task<AbuseWorkflowCommandResult> completionTask =
            completionService.CompleteAsync(executionCommand);

        firstCompletedTask = await Task.WhenAny(
            completionTask,
            Task.Delay(TimeSpan.FromMilliseconds(300)));
        Assert.NotSame(completionTask, firstCompletedTask);

        await completionLockTransaction.CommitAsync();
        AbuseWorkflowCommandResult completionResult =
            await completionTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(completionResult.Succeeded);

        await using AdminDbContext conflictingSanctionDbContext =
            new(CreateOptions<AdminDbContext>());
        ApplyUserSanctionService conflictingSanctionService = new(
            conflictingSanctionDbContext,
            new FixedTimeProvider(executionTime));
        ApplyUserSanctionResult conflictingSanctionResult =
            await conflictingSanctionService.ApplyAsync(
                new ApplyUserSanctionCommand(
                    reviewingAdminUserAccountId,
                    customerUserAccountId,
                    UserSanctionType.TemporaryBan,
                    "Conflicting post-closure sanction.",
                    executionTime.AddHours(25)));

        Assert.False(conflictingSanctionResult.Succeeded);
        Assert.Equal("USER_ACTIVE_SANCTION_EXISTS", conflictingSanctionResult.ErrorCode);
        Assert.Equal(
            1,
            await conflictingSanctionDbContext.UserSanctions.CountAsync(
                entity => entity.UserAccountId == customerUserAccountId
                    && entity.RevokedAtUtc == null
                    && entity.Type != UserSanctionType.Warning));
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
    public async Task ApproveAppointmentRequestWaitsForRowLockBeforeStateTransition()
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
        AppointmentRequest request = CreateRequest(
            tenantId,
            Guid.CreateVersion7(),
            branchId,
            staffMemberId,
            resourceId);

        await using (BookingDbContext seedDbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor))
        {
            seedDbContext.AppointmentRequests.Add(request);
            await seedDbContext.SaveChangesAsync();
        }

        await using NpgsqlConnection lockConnection = new(DatabaseConnectionString);
        await lockConnection.OpenAsync();
        await using NpgsqlTransaction lockTransaction =
            await lockConnection.BeginTransactionAsync();
        await LockAppointmentRequestRowAsync(
            lockConnection,
            lockTransaction,
            tenantId,
            request.Id);

        await using BookingDbContext bookingDbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor);
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

        Task<AppointmentRequestDecisionResult> approvalTask =
            service.ApproveAsync(
                request.Id,
                approverUserAccountId);

        Task firstCompletedTask = await Task.WhenAny(
            approvalTask,
            Task.Delay(TimeSpan.FromMilliseconds(300)));
        Assert.NotSame(approvalTask, firstCompletedTask);

        await lockTransaction.CommitAsync();

        AppointmentRequestDecisionResult result =
            await approvalTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.AppointmentId);
    }

    [Fact]
    public async Task ExpireAppointmentRequestsServiceSkipsRowsLockedByConcurrentDecision()
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

        await using (BookingDbContext seedDbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor))
        {
            seedDbContext.AppointmentRequests.Add(request);
            await seedDbContext.SaveChangesAsync();
        }

        await using NpgsqlConnection lockConnection = new(DatabaseConnectionString);
        await lockConnection.OpenAsync();
        await using NpgsqlTransaction lockTransaction =
            await lockConnection.BeginTransactionAsync();
        await LockAppointmentRequestRowAsync(
            lockConnection,
            lockTransaction,
            tenantId,
            request.Id);

        await using BookingDbContext lockedDbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor);
        ExpireAppointmentRequestsService lockedService = new(
            lockedDbContext,
            tenantContextAccessor,
            new FixedTimeProvider(testTime));

        int skippedCount =
            await lockedService.ExpireDueAsync().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(0, skippedCount);

        await lockTransaction.CommitAsync();

        await using BookingDbContext expiryDbContext =
            new(CreateOptions<BookingDbContext>(), tenantContextAccessor);
        ExpireAppointmentRequestsService expiryService = new(
            expiryDbContext,
            tenantContextAccessor,
            new FixedTimeProvider(testTime));

        int expiredCount = await expiryService.ExpireDueAsync();

        Assert.Equal(1, expiredCount);
    }

    [Fact]
    public async Task DeclineAppointmentRequestConcurrentSameIdempotencyKeyReplaysSingleDecision()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        BookingIdempotencyContext idempotency = new(
            "decline-key-hash",
            "decline-request-hash");
        AppointmentRequest request = CreateRequest(
            tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());

        await using (BookingDbContext seedDbContext =
            new(CreateOptions<BookingDbContext>(), new TenantContextAccessor { TenantId = tenantId }))
        {
            seedDbContext.AppointmentRequests.Add(request);
            await seedDbContext.SaveChangesAsync();
        }

        DeclineAppointmentRequestService firstService =
            CreateDeclineAppointmentRequestService(tenantId);
        DeclineAppointmentRequestService secondService =
            CreateDeclineAppointmentRequestService(tenantId);

        AppointmentRequestDecisionResult[] results = await Task.WhenAll(
            firstService.DeclineAsync(request.Id, actorUserAccountId, idempotency),
            secondService.DeclineAsync(request.Id, actorUserAccountId, idempotency));

        Assert.All(results, result => Assert.True(result.Succeeded));

        await using BookingDbContext verifyDbContext =
            new(CreateOptions<BookingDbContext>(), new TenantContextAccessor { TenantId = tenantId });
        AppointmentRequestStatus status = await verifyDbContext.AppointmentRequests
            .Where(entity => entity.Id == request.Id)
            .Select(entity => entity.Status)
            .SingleAsync();
        int idempotencyRecordCount = await verifyDbContext.IdempotencyRecords.CountAsync();

        Assert.Equal(AppointmentRequestStatus.Declined, status);
        Assert.Equal(1, idempotencyRecordCount);
    }

    [Fact]
    public async Task CancelAppointmentRequestConcurrentSameIdempotencyKeyReplaysSingleDecision()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerUserAccountId = Guid.CreateVersion7();
        BookingIdempotencyContext idempotency = new(
            "cancel-key-hash",
            "cancel-request-hash");
        AppointmentRequest request = CreateRequest(
            tenantId,
            customerUserAccountId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());

        await using (BookingDbContext seedDbContext =
            new(CreateOptions<BookingDbContext>(), new TenantContextAccessor { TenantId = tenantId }))
        {
            seedDbContext.AppointmentRequests.Add(request);
            await seedDbContext.SaveChangesAsync();
        }

        CancelAppointmentRequestService firstService =
            CreateCancelAppointmentRequestService(tenantId);
        CancelAppointmentRequestService secondService =
            CreateCancelAppointmentRequestService(tenantId);

        AppointmentRequestDecisionResult[] results = await Task.WhenAll(
            firstService.CancelAsync(request.Id, customerUserAccountId, idempotency),
            secondService.CancelAsync(request.Id, customerUserAccountId, idempotency));

        Assert.All(results, result => Assert.True(result.Succeeded));

        await using BookingDbContext verifyDbContext =
            new(CreateOptions<BookingDbContext>(), new TenantContextAccessor { TenantId = tenantId });
        AppointmentRequestStatus status = await verifyDbContext.AppointmentRequests
            .Where(entity => entity.Id == request.Id)
            .Select(entity => entity.Status)
            .SingleAsync();
        int idempotencyRecordCount = await verifyDbContext.IdempotencyRecords.CountAsync();

        Assert.Equal(AppointmentRequestStatus.CancelledByCustomer, status);
        Assert.Equal(1, idempotencyRecordCount);
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

    private CancelAppointmentRequestService CreateCancelAppointmentRequestService(Guid tenantId)
    {
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        return new CancelAppointmentRequestService(
            new BookingDbContext(CreateOptions<BookingDbContext>(), tenantContextAccessor),
            new AdminAuditLogRecorder(new AdminDbContext(CreateOptions<AdminDbContext>())),
            tenantContextAccessor,
            new FixedTimeProvider(testTime));
    }

    private DeclineAppointmentRequestService CreateDeclineAppointmentRequestService(Guid tenantId)
    {
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        return new DeclineAppointmentRequestService(
            new BookingDbContext(CreateOptions<BookingDbContext>(), tenantContextAccessor),
            new AdminAuditLogRecorder(new AdminDbContext(CreateOptions<AdminDbContext>())),
            tenantContextAccessor,
            new FixedTimeProvider(testTime));
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

    private static async Task LockAppointmentRequestRowAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid tenantId,
        Guid appointmentRequestId)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT 1
            FROM booking."AppointmentRequests"
            WHERE "TenantId" = @tenantId
                AND "Id" = @appointmentRequestId
            FOR UPDATE
            """;
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("appointmentRequestId", appointmentRequestId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AcquireAbuseUserWorkflowLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid userAccountId)
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "SELECT pg_advisory_xact_lock(hashtextextended(@lockKey, 0))";
        command.Parameters.AddWithValue(
            "lockKey",
            $"abuse-user-workflow:{userAccountId:D}");
        await command.ExecuteNonQueryAsync();
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
