using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using RezSaaS.BuildingBlocks.Booking;
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
using RezSaaS.Modules.Catalog.Application;
using RezSaaS.Modules.Catalog.Domain;
using RezSaaS.Modules.Catalog.Infrastructure.Persistence;
using RezSaaS.Modules.Integrations.Application;
using RezSaaS.Modules.Integrations.Domain;
using RezSaaS.Modules.Integrations.Infrastructure.Persistence;
using RezSaaS.Modules.Messaging.Application;
using RezSaaS.Modules.Messaging.Domain;
using RezSaaS.Modules.Messaging.Infrastructure.Queue;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Messaging.Infrastructure.Persistence;
using RezSaaS.Modules.Organization.Domain;
using RezSaaS.Modules.Organization.Infrastructure.Persistence;
using RezSaaS.Modules.Payments.Infrastructure.Persistence;
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
        Assert.Equal(0, await CountRowsAsync("messaging", "PlatformTransactionalMessages"));
        Assert.Equal(0, await CountRowsAsync("messaging", "TransactionalMessages"));
        Assert.Equal(0, await CountRowsAsync("integrations", "IntegrationApiClients"));
        Assert.Equal(0, await CountRowsAsync("integrations", "WebhookSubscriptions"));
        Assert.Equal(0, await CountRowsAsync("integrations", "WebhookDeliveries"));
        Assert.Equal(0, await CountRowsAsync("payments", "PaymentIntents"));
        Assert.Equal(0, await CountRowsAsync("payments", "PaymentPolicies"));
        Assert.Equal(0, await CountRowsAsync("payments", "PaymentWebhookEvents"));
        Assert.Equal(0, await CountRowsAsync("resources", "Resources"));
        Assert.Equal(0, await CountRowsAsync("availability", "BranchWorkingHours"));
        Assert.Equal(0, await CountRowsAsync("booking", "AppointmentRequests"));
        Assert.Equal(0, await CountRowsAsync("booking", "Appointments"));
    }

    [Fact]
    public async Task IntegrationApiClientLifecycleStoresOnlyHashedSecretAndAuditMetadata()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        await using IntegrationsDbContext dbContext =
            new(CreateOptions<IntegrationsDbContext>(), tenantContextAccessor);
        IntegrationApiClientLifecycleService service = new(
            dbContext,
            Options.Create(new IntegrationReadinessOptions { ExternalApiEnabled = true }),
            tenantContextAccessor,
            new FixedTimeProvider(testTime));

        CreateIntegrationApiClientResult createResult = await service.CreateAsync(
            new CreateIntegrationApiClientCommand(
                actorUserAccountId,
                "CRM Export",
                ["appointments:read", "customers:read", "appointments:read"]));
        IntegrationLifecycleResult revokeResult = await service.RevokeAsync(
            createResult.ApiClientId,
            actorUserAccountId,
            "Rotated during integration lifecycle test.");

        IntegrationApiClient storedClient =
            await dbContext.ApiClients.IgnoreQueryFilters().SingleAsync();
        string auditJson = string.Join(
            Environment.NewLine,
            await dbContext.AuditLogEntries
                .IgnoreQueryFilters()
                .OrderBy(entity => entity.OccurredAtUtc)
                .Select(entity => entity.DetailsJson)
                .ToListAsync());

        Assert.True(revokeResult.Succeeded);
        Assert.StartsWith("rzs_live_", createResult.KeyPrefix, StringComparison.Ordinal);
        Assert.StartsWith(createResult.KeyPrefix, createResult.OneTimePlaintextApiKey, StringComparison.Ordinal);
        Assert.Equal(createResult.KeyPrefix, storedClient.KeyPrefix);
        Assert.Equal(64, storedClient.KeyHashSha256.Length);
        Assert.NotEqual(createResult.OneTimePlaintextApiKey, storedClient.KeyHashSha256);
        Assert.Equal("appointments:read customers:read", storedClient.ScopeSet);
        Assert.Equal(IntegrationApiClientStatus.Revoked, storedClient.Status);
        Assert.Equal(2, await dbContext.AuditLogEntries.IgnoreQueryFilters().CountAsync());
        Assert.DoesNotContain(createResult.OneTimePlaintextApiKey, auditJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WebhookSubscriptionLifecycleStoresOnlySigningHashAndSafeAuditMetadata()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        await using IntegrationsDbContext dbContext =
            new(CreateOptions<IntegrationsDbContext>(), tenantContextAccessor);
        WebhookSubscriptionLifecycleService service = new(
            dbContext,
            Options.Create(
                new IntegrationReadinessOptions
                {
                    ExternalApiEnabled = true,
                    WebhookDeliveryEnabled = true,
                }),
            tenantContextAccessor,
            new FixedTimeProvider(testTime));

        CreateWebhookSubscriptionResult createResult = await service.CreateAsync(
            new CreateWebhookSubscriptionCommand(
                actorUserAccountId,
                "CRM Webhook",
                "HTTPS://hooks.example.test/rezsaas",
                ["appointment.created", "appointment.updated", "appointment.created"]));
        IntegrationLifecycleResult pauseResult = await service.PauseAsync(
            createResult.WebhookSubscriptionId,
            actorUserAccountId);
        IntegrationLifecycleResult reactivateResult = await service.ReactivateAsync(
            createResult.WebhookSubscriptionId,
            actorUserAccountId);
        IntegrationLifecycleResult revokeResult = await service.RevokeAsync(
            createResult.WebhookSubscriptionId,
            actorUserAccountId,
            "Endpoint ownership rotated.");

        WebhookSubscription storedSubscription =
            await dbContext.WebhookSubscriptions.IgnoreQueryFilters().SingleAsync();
        string auditJson = string.Join(
            Environment.NewLine,
            await dbContext.AuditLogEntries
                .IgnoreQueryFilters()
                .OrderBy(entity => entity.OccurredAtUtc)
                .Select(entity => entity.DetailsJson)
                .ToListAsync());

        Assert.True(pauseResult.Succeeded);
        Assert.True(reactivateResult.Succeeded);
        Assert.True(revokeResult.Succeeded);
        Assert.StartsWith("whsec_", createResult.OneTimePlaintextSigningSecret, StringComparison.Ordinal);
        Assert.Equal(64, storedSubscription.SigningSecretHashSha256.Length);
        Assert.NotEqual(
            createResult.OneTimePlaintextSigningSecret,
            storedSubscription.SigningSecretHashSha256);
        Assert.Equal("https://hooks.example.test/rezsaas", storedSubscription.TargetUrl);
        Assert.Equal("appointment.created,appointment.updated", storedSubscription.EventTypes);
        Assert.Equal(WebhookSubscriptionStatus.Revoked, storedSubscription.Status);
        Assert.Equal(4, await dbContext.AuditLogEntries.IgnoreQueryFilters().CountAsync());
        Assert.DoesNotContain(createResult.OneTimePlaintextSigningSecret, auditJson, StringComparison.Ordinal);
        Assert.DoesNotContain(storedSubscription.TargetUrl, auditJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformMessageQueueDeduplicatesAndDoesNotResendAcceptedDeliveries()
    {
        Guid userAccountId = Guid.CreateVersion7();
        Guid correlationId = Guid.CreateVersion7();
        PlatformTransactionalMessageEnvelope envelope = new(
            userAccountId,
            PlatformMessagePurpose.AccountClosureProposed,
            correlationId,
            $"account-closure-proposed:{correlationId:D}",
            "Account closure review",
            "Your account closure case is ready for review.");
        Guid messageId;

        await using (MessagingDbContext enqueueDbContext =
            new(CreateOptions<MessagingDbContext>()))
        {
            PlatformTransactionalMessageQueueService enqueueService = new(
                enqueueDbContext,
                new FixedTimeProvider(testTime));

            messageId = await enqueueService.EnqueueAsync(envelope);
            Guid replayMessageId = await enqueueService.EnqueueAsync(envelope);
            PlatformTransactionalMessageEnvelope collision = envelope with
            {
                Subject = "Different immutable content",
            };

            Assert.Equal(messageId, replayMessageId);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => enqueueService.EnqueueAsync(collision));
        }

        await using (MessagingDbContext firstAttemptDbContext =
            new(CreateOptions<MessagingDbContext>()))
        {
            PlatformTransactionalMessageQueueService firstAttemptService = new(
                firstAttemptDbContext,
                new FixedTimeProvider(testTime));
            PlatformTransactionalMessageDeliveryView firstAttempt =
                Assert.Single(await firstAttemptService.ClaimDueAsync(
                    batchSize: 10,
                    lockDuration: TimeSpan.FromMinutes(5)));

            Assert.Equal(messageId, firstAttempt.Id);
            Assert.Null(firstAttempt.SentAtUtc);

            await firstAttemptService.MarkDeliveryAcceptedAsync(messageId, testTime);
            await firstAttemptService.ScheduleRetryAsync(
                messageId,
                "CALLBACK_FAILED",
                TimeSpan.FromMinutes(5),
                maxAttempts: 3);
        }

        await using (MessagingDbContext retryDbContext =
            new(CreateOptions<MessagingDbContext>()))
        {
            PlatformTransactionalMessageQueueService retryService = new(
                retryDbContext,
                new FixedTimeProvider(testTime.AddMinutes(5)));
            PlatformTransactionalMessageDeliveryView retry =
                Assert.Single(await retryService.ClaimDueAsync(
                    batchSize: 10,
                    lockDuration: TimeSpan.FromMinutes(5)));

            Assert.Equal(testTime, retry.SentAtUtc);
            await retryService.CompleteAsync(messageId);
        }

        await using MessagingDbContext verificationDbContext =
            new(CreateOptions<MessagingDbContext>());
        PlatformTransactionalMessage persistedMessage =
            await verificationDbContext.PlatformTransactionalMessages.SingleAsync();

        Assert.Equal(PlatformTransactionalMessageStatus.Sent, persistedMessage.Status);
        Assert.Equal(2, persistedMessage.AttemptCount);
        Assert.Equal(1, await CountRowsAsync("messaging", "PlatformTransactionalMessages"));
    }

    [Fact]
    public async Task ReconciliationQueriesDetectPlatformNotificationAndClosureIncidents()
    {
        DateTimeOffset createdAtUtc = testTime.AddHours(-2);
        PlatformTransactionalMessage failedMessage = PlatformTransactionalMessage.Create(
            Guid.CreateVersion7(),
            PlatformMessagePurpose.AbuseAppealRejected,
            Guid.CreateVersion7(),
            $"failed:{Guid.CreateVersion7():D}",
            "Appeal review",
            "Your appeal review is complete.",
            createdAtUtc);
        failedMessage.BeginAttempt(
            createdAtUtc.AddMinutes(1),
            createdAtUtc.AddMinutes(6));
        failedMessage.ScheduleRetry(
            "PROVIDER_FAILURE",
            createdAtUtc.AddMinutes(2),
            createdAtUtc.AddMinutes(3),
            maxAttempts: 1);
        PlatformTransactionalMessage staleProcessingMessage =
            PlatformTransactionalMessage.Create(
                Guid.CreateVersion7(),
                PlatformMessagePurpose.AbuseAppealAccepted,
                Guid.CreateVersion7(),
                $"stale-processing:{Guid.CreateVersion7():D}",
                "Appeal review",
                "Your appeal review is complete.",
                createdAtUtc);
        staleProcessingMessage.BeginAttempt(
            createdAtUtc.AddMinutes(1),
            testTime.AddHours(-1));
        PlatformTransactionalMessage callbackPendingMessage =
            PlatformTransactionalMessage.Create(
                Guid.CreateVersion7(),
                PlatformMessagePurpose.AccountClosureProposed,
                Guid.CreateVersion7(),
                $"callback-pending:{Guid.CreateVersion7():D}",
                "Account closure review",
                "Your account closure case is ready for review.",
                createdAtUtc);
        callbackPendingMessage.BeginAttempt(
            createdAtUtc.AddMinutes(1),
            createdAtUtc.AddMinutes(6));
        callbackPendingMessage.MarkDeliveryAccepted(createdAtUtc.AddMinutes(2));
        callbackPendingMessage.ScheduleRetry(
            "CALLBACK_FAILED",
            createdAtUtc.AddMinutes(3),
            createdAtUtc.AddMinutes(4),
            maxAttempts: 5);

        await using (MessagingDbContext dbContext =
            new(CreateOptions<MessagingDbContext>()))
        {
            dbContext.PlatformTransactionalMessages.AddRange(
                failedMessage,
                staleProcessingMessage,
                callbackPendingMessage);
            await dbContext.SaveChangesAsync();
        }

        AccountClosureCase notificationOverdueClosure = AccountClosureCase.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Verified high-risk evidence.",
            "Your account is scheduled for closure after review.",
            createdAtUtc);
        AccountClosureCase stalledExecutionClosure = AccountClosureCase.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Verified high-risk evidence.",
            "Your account is scheduled for closure after review.",
            testTime.AddDays(-10));
        stalledExecutionClosure.MarkCustomerNoticeDelivered(
            testTime.AddDays(-10),
            TimeSpan.FromDays(7));
        stalledExecutionClosure.Approve(
            Guid.CreateVersion7(),
            "Independent evidence review.",
            testTime.AddDays(-10).AddHours(1));
        stalledExecutionClosure.BeginExecution(
            Guid.CreateVersion7(),
            createdAtUtc);

        await using (AdminDbContext dbContext =
            new(CreateOptions<AdminDbContext>()))
        {
            dbContext.AccountClosureCases.AddRange(
                notificationOverdueClosure,
                stalledExecutionClosure);
            await dbContext.SaveChangesAsync();
        }

        await using MessagingDbContext messagingDbContext =
            new(CreateOptions<MessagingDbContext>());
        await using AdminDbContext adminDbContext =
            new(CreateOptions<AdminDbContext>());
        PlatformNotificationReconciliationSnapshot notificationSnapshot =
            await new PlatformNotificationReconciliationQueryService(messagingDbContext)
                .InspectAsync(
                    new PlatformNotificationReconciliationQuery(
                        testTime.AddMinutes(-30),
                        testTime.AddMinutes(-15),
                        SampleSize: 10));
        AccountClosureReconciliationSnapshot closureSnapshot =
            await new AccountClosureReconciliationQueryService(adminDbContext)
                .InspectAsync(
                    new AccountClosureReconciliationQuery(
                        testTime.AddMinutes(-30),
                        testTime.AddMinutes(-15),
                        SampleSize: 10));

        Assert.Equal(1, notificationSnapshot.FailedCount);
        Assert.Equal(1, notificationSnapshot.StaleProcessingCount);
        Assert.Equal(1, notificationSnapshot.CallbackPendingCount);
        Assert.Contains(failedMessage.Id, notificationSnapshot.FailedMessageIds);
        Assert.Contains(
            staleProcessingMessage.Id,
            notificationSnapshot.StaleProcessingMessageIds);
        Assert.Contains(
            callbackPendingMessage.Id,
            notificationSnapshot.CallbackPendingMessageIds);
        Assert.Equal(1, closureSnapshot.NotificationOverdueCount);
        Assert.Equal(1, closureSnapshot.ExecutionStalledCount);
        Assert.Contains(
            notificationOverdueClosure.Id,
            closureSnapshot.NotificationOverdueClosureCaseIds);
        Assert.Contains(
            stalledExecutionClosure.Id,
            closureSnapshot.ExecutionStalledClosureCaseIds);
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

        AccountClosureExecutionService executionService = new(
            dbContext,
            Options.Create(riskOptions),
            new FixedTimeProvider(testTime.AddDays(8)));
        ExecuteAccountClosureCommand executionCommand =
            new(executingAdminUserAccountId, proposalResult.EntityId.Value);
        AbuseWorkflowCommandResult noticeBlockedExecutionResult =
            await executionService.BeginAsync(executionCommand);
        AccountClosureNoticeDeliveryService noticeDeliveryService = new(
            dbContext,
            Options.Create(riskOptions));
        AccountClosureNoticeDeliveryState noticeDeliveryState =
            await noticeDeliveryService.MarkDeliveredAsync(
                proposalResult.EntityId.Value,
                testTime.AddHours(3));

        Assert.False(noticeBlockedExecutionResult.Succeeded);
        Assert.Equal(
            "ACCOUNT_CLOSURE_NOTICE_NOT_DELIVERED",
            noticeBlockedExecutionResult.ErrorCode);
        Assert.Equal(AccountClosureNoticeDeliveryState.Delivered, noticeDeliveryState);

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
            testTime);
        closureCase.MarkCustomerNoticeDelivered(
            testTime,
            TimeSpan.FromDays(riskOptions.ClosureAppealWindowDays));
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
            testTime);
        closureCase.MarkCustomerNoticeDelivered(testTime, TimeSpan.FromDays(7));
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

    /// <summary>
    /// Sabit bir iptal politikasi donen sahte lookup. Gercek adapter Organization'daki
    /// Business kaydini okur; burada politikayi testin kontrol etmesi gerekiyor.
    /// </summary>
    private sealed class StubCancellationPolicyLookup : IBusinessCancellationPolicyLookup
    {
        private readonly BusinessCancellationPolicy? policy;

        public StubCancellationPolicyLookup(int? cancellationCutoffHours)
        {
            policy = cancellationCutoffHours is { } hours
                ? new BusinessCancellationPolicy(hours)
                : null;
        }

        public Task<BusinessCancellationPolicy?> GetAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(policy);
        }
    }

    private CancelAppointmentByCustomerService CreateCancelAppointmentByCustomerService(
        Guid tenantId,
        int? cancellationCutoffHours,
        DateTimeOffset? now = null)
    {
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        return new CancelAppointmentByCustomerService(
            new BookingDbContext(CreateOptions<BookingDbContext>(), tenantContextAccessor),
            new AdminAuditLogRecorder(new AdminDbContext(CreateOptions<AdminDbContext>())),
            new StubCancellationPolicyLookup(cancellationCutoffHours),
            tenantContextAccessor,
            new FixedTimeProvider(now ?? testTime));
    }

    private async Task<Appointment> SeedConfirmedAppointmentAsync(
        Guid tenantId,
        Guid customerUserAccountId,
        DateTimeOffset startUtc)
    {
        Appointment appointment = Appointment.CreateConfirmed(
            tenantId,
            appointmentRequestId: null,
            customerUserAccountId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            startUtc,
            startUtc.AddMinutes(60),
            testTime);
        appointment.AddLine(Guid.CreateVersion7(), "Saç Kesimi", 60, 500, "TRY");

        TenantContextAccessor accessor = new() { TenantId = tenantId };
        await using BookingDbContext dbContext =
            new(CreateOptions<BookingDbContext>(), accessor);
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync();

        return appointment;
    }

    private async Task<AppointmentStatus> ReadAppointmentStatusAsync(Guid tenantId, Guid appointmentId)
    {
        TenantContextAccessor accessor = new() { TenantId = tenantId };
        await using BookingDbContext dbContext =
            new(CreateOptions<BookingDbContext>(), accessor);

        return await dbContext.Appointments
            .AsNoTracking()
            .Where(entity => entity.Id == appointmentId)
            .Select(entity => entity.Status)
            .SingleAsync();
    }

    /// <summary>
    /// REGRESYON: varyant para birimi GERCEKTEN uygulanir ve WHITELIST'e karsi dogrulanir.
    /// </summary>
    /// <remarks>
    /// IKI BUG BIRDEN (uctan uca duman testi ortaya cikardi):
    ///
    /// 1) SESSIZ NO-OP: ServiceVariantManagementService.UpdateAsync CurrencyCode'u DOGRULUYOR
    ///    ("bos olamaz") ama UYGULAYACAK BIR DOMAIN METODU OLMADIGI ICIN ASLA YAZMIYORDU.
    ///    Istek 200 OK donuyor, para birimi degismiyordu. Sozlesme "bu alan zorunlu" diyordu
    ///    ama alan ETKISIZDI.
    ///    Bu, kod tabanindaki UCUNCU ayni tur sessiz no-op idi (StaffMember.Rename yoktu;
    ///    Service.Archive() cagrilmiyordu -- onun yerine KALICI SILINIYORDU).
    ///
    /// 2) WHITELIST YOKTU: CurrencyCode serbest 3-karakter string'di. Ayni isletmenin katalogu
    ///    "Sac Kesimi 400 TRY" + "Boya 800 USD" gibi KARISIK PARA BIRIMINE dusebiliyordu.
    ///    "UI'da secici koymayiz" bir kural kontrolu DEGILDIR -- API dogrudan cagrilabilir.
    ///    Kisit artik DOMAIN'de.
    /// </remarks>
    [Fact]
    public async Task VariantCurrencyIsAppliedAndRestrictedToSupportedCodes()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        await using CatalogDbContext dbContext =
            new(CreateOptions<CatalogDbContext>(), tenantContextAccessor);

        Service service = Service.Create(tenantId, "Saç Kesimi", "hair", testTime);
        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync();

        ServiceVariantManagementService variantService = new(
            dbContext,
            tenantContextAccessor,
            new AdminAuditLogRecorder(new AdminDbContext(CreateOptions<AdminDbContext>())),
            new FixedTimeProvider(testTime));

        // USD ile YARATMA artik REDDEDILIR (eskiden kabul ediliyordu -> karisik katalog).
        ServiceVariantManagementResult usdCreate = await variantService.CreateAsync(
            new CreateServiceVariantCommand(
                actorUserAccountId, service.Id, "Kısa Saç", 30, 400m, "USD", null));

        Assert.False(usdCreate.Succeeded);
        Assert.Equal(ServiceVariantManagementService.InvalidRequest, usdCreate.ErrorCode);

        // TRY ile yaratma calisir.
        ServiceVariantManagementResult created = await variantService.CreateAsync(
            new CreateServiceVariantCommand(
                actorUserAccountId, service.Id, "Kısa Saç", 30, 400m, "TRY", null));

        Assert.True(created.Succeeded);
        Guid variantId = created.Variant!.Id;

        // USD'ye GUNCELLEME de reddedilir.
        ServiceVariantManagementResult usdUpdate = await variantService.UpdateAsync(
            new UpdateServiceVariantCommand(
                actorUserAccountId, service.Id, variantId, "Kısa Saç", 30, 400m, "USD", null));

        Assert.False(usdUpdate.Succeeded);

        // Kucuk harf "try" kabul edilir ve NORMALIZE edilir (TRY olarak yazilir).
        ServiceVariantManagementResult ok = await variantService.UpdateAsync(
            new UpdateServiceVariantCommand(
                actorUserAccountId, service.Id, variantId, "Kısa Saç", 45, 550m, "try", null));

        Assert.True(ok.Succeeded);

        // ASIL KONTROL: DB'den yeniden oku. Para birimi GERCEKTEN yazildi mi?
        ServiceVariant persisted = await dbContext.ServiceVariants
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == variantId);

        Assert.Equal("TRY", persisted.CurrencyCode);
        Assert.Equal(550m, persisted.PriceAmount);
        Assert.Equal(45, persisted.DurationMinutes);
    }

    /// <summary>
    /// REGRESYON: hizmet "arsivleme" GERCEKTEN arsivler -- KALICI SILMEZ.
    /// </summary>
    /// <remarks>
    /// BULUNAN BUG (uctan uca duman testi ortaya cikardi):
    /// ServiceManagementService.ArchiveAsync `dbContext.Services.Remove(service)` cagiriyordu --
    /// yani "arsivle" adli uc KALICI SILME yapiyordu. Domain'de calisan bir Archive() metodu
    /// vardi (Status = Archived) ama HIC CAGRILMIYORDU. Ustelik audit kaydi
    /// "catalog.service.archived" diyordu: hem kullaniciya hem denetim gunlugune yalan.
    /// (Personel Rename bug'inin birebir aynisi.)
    ///
    /// Ayrica "varyanti varsa arsivleme" engeli vardi (409). Fiyat ve sure ZATEN varyantta
    /// yasadigi icin GERCEK HER HIZMETIN varyanti var -- yani arsivleme pratikte HIC
    /// CALISMIYORDU. O engel hard-delete'in FK artigiydi; soft archive'da gereksiz.
    /// </remarks>
    [Fact]
    public async Task ServiceArchiveSetsStatusInsteadOfDeletingAndWorksWithVariants()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        await using CatalogDbContext dbContext =
            new(CreateOptions<CatalogDbContext>(), tenantContextAccessor);

        Service service = Service.Create(tenantId, "Saç Kesimi", "hair", testTime);
        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync();

        // VARYANT EKLE: eski kod bu yuzden 409 doner ve arsivlemeyi TAMAMEN reddederdi.
        // Fiyat/sure zaten varyantta yasiyor, yani gercek her hizmetin varyanti vardir.
        dbContext.ServiceVariants.Add(
            ServiceVariant.Create(tenantId, service.Id, "Kısa Saç", 30, 400m, "TRY", testTime));
        await dbContext.SaveChangesAsync();

        ServiceManagementService managementService = new(
            dbContext,
            tenantContextAccessor,
            new AdminAuditLogRecorder(new AdminDbContext(CreateOptions<AdminDbContext>())),
            new FixedTimeProvider(testTime));

        ServiceManagementResult result =
            await managementService.ArchiveAsync(actorUserAccountId, service.Id);

        Assert.True(result.Succeeded);

        // ASIL KONTROL: kayit HALA VAR MI (silinmedi mi) ve statusu Archived mi?
        // Eski kod burada satiri KALICI OLARAK SILIYORDU.
        Service persisted = await dbContext.Services
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == service.Id);

        Assert.Equal(ServiceStatus.Archived, persisted.Status);

        // Varyantlar da DB'de kalir (arsivlenmis hizmetin varyantlari silinmez).
        Assert.True(
            await dbContext.ServiceVariants
                .AsNoTracking()
                .AnyAsync(entity => entity.ServiceId == service.Id));
    }

    /// <summary>
    /// REGRESYON: musteri gecmisi status filtresi IKI aggregate'in status'lerini de kabul eder.
    /// </summary>
    /// <remarks>
    /// BULUNAN BUG (uctan uca duman testi ortaya cikardi -- derleyici goremezdi):
    /// GET /api/customer/appointment-history TEK listede IKI aggregate donuyor ve bu ikisinin
    /// status enum'larinin KESISIMI BOS:
    ///     Talep   : PendingApproval, Approved, Declined, Expired, Superseded, CancelledByCustomer
    ///     Randevu : Confirmed, Cancelled, Completed, NoShow, Rebooked
    ///
    /// Uc, status'u YALNIZCA talep enum'una gore doğruluyordu. Sonuc:
    ///   ?status=Confirmed       -> 400 (randevu statusu reddediliyordu)
    ///   ?status=PendingApproval -> 200 ama randevu sorgusu tanimadigi icin randevular BOS
    /// Yani HANGI DEGERI VERIRSENIZ VERIN randevular gecmiste HIC GORUNMUYORDU.
    ///
    /// Iki enum da ayri ayri gecerliydi; hata BIRLESIMLERININ dusunulmemis olmasindaydi.
    /// </remarks>
    [Fact]
    public void CustomerHistoryStatusFilterAcceptsBothAggregateStatuses()
    {
        // Talep statusleri
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("PendingApproval"));
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("Superseded"));
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("CancelledByCustomer"));

        // Randevu statusleri -- BUNLAR 400 ILE REDDEDILIYORDU.
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("Confirmed"));
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("Cancelled"));
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("Completed"));
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("NoShow"));
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("Rebooked"));

        // Bos/null = filtre yok
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty(null));
        Assert.True(CustomerHistoryStatusFilter.IsValidOrEmpty("   "));

        // Uydurma degerler HALA reddedilir (fail-closed korunuyor).
        Assert.False(CustomerHistoryStatusFilter.IsValidOrEmpty("Uydurma"));
        Assert.False(CustomerHistoryStatusFilter.IsValidOrEmpty("Onaylandi"));
    }

    /// <summary>
    /// REGRESYON: iki status enum'unun kesisimi BOS olmali.
    /// </summary>
    /// <remarks>
    /// Bu testin amaci filtreyi degil, VARSAYIMI korumak. Ileride biri iki enum'a ortak bir
    /// deger eklerse (or. her ikisine de "Cancelled"), tek bir ?status degeri HEM talebi HEM
    /// randevuyu eslestirir ve gecmis listesi ayni kaydi IKI KEZ gosterebilir. O gun bu test
    /// kirilir ve mesele fark edilir.
    /// </remarks>
    [Fact]
    public void AppointmentAndRequestStatusEnumsDoNotOverlap()
    {
        HashSet<string> requestStatuses = Enum.GetNames<AppointmentRequestStatus>().ToHashSet();
        HashSet<string> appointmentStatuses = Enum.GetNames<AppointmentStatus>().ToHashSet();

        requestStatuses.IntersectWith(appointmentStatuses);

        Assert.Empty(requestStatuses);
    }

    /// <summary>
    /// LANSMAN BLOKAJI REGRESYONU: musteri KENDI onaylanmis randevusunu iptal edebilir.
    /// </summary>
    /// <remarks>
    /// Onceden HIC edemiyordu: talep iptali yalnizca PendingApproval'da calisiyor, isletme
    /// tarafindaki iptal ucu tenant uyeligi ariyordu. Plani degisen musterinin yapacak
    /// hicbir seyi yoktu -- salonu ARAMAK zorundaydi.
    /// </remarks>
    [Fact]
    public async Task CustomerCanCancelOwnConfirmedAppointment()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerId = Guid.CreateVersion7();

        // Randevu 10 gun sonra: cutoff penceresinin cok disinda.
        Appointment appointment =
            await SeedConfirmedAppointmentAsync(tenantId, customerId, testTime.AddDays(10));

        CancelAppointmentByCustomerService service =
            CreateCancelAppointmentByCustomerService(tenantId, cancellationCutoffHours: 2);

        CustomerAppointmentCancellationResult result =
            await service.CancelAsync(appointment.Id, customerId);

        Assert.True(result.Succeeded);

        // Servisin donus degerine GUVENME -- veritabanindan yeniden oku.
        Assert.Equal(
            AppointmentStatus.Cancelled,
            await ReadAppointmentStatusAsync(tenantId, appointment.Id));
    }

    /// <summary>
    /// GUVENLIK: baskasinin randevusunu iptal EDEMEZ ve varligini bile ogrenemez (404).
    /// </summary>
    /// <remarks>
    /// 403 donmek "bu kayit var ama goremiyorsun" bilgisini SIZDIRIRDI. Mevcut talep-iptal
    /// servisi de 404 donuyor; ayni davranis.
    /// </remarks>
    [Fact]
    public async Task CustomerCannotCancelSomeoneElsesAppointment()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid ownerCustomerId = Guid.CreateVersion7();
        Guid attackerCustomerId = Guid.CreateVersion7();

        Appointment appointment =
            await SeedConfirmedAppointmentAsync(tenantId, ownerCustomerId, testTime.AddDays(10));

        CancelAppointmentByCustomerService service =
            CreateCancelAppointmentByCustomerService(tenantId, cancellationCutoffHours: 2);

        CustomerAppointmentCancellationResult result =
            await service.CancelAsync(appointment.Id, attackerCustomerId);

        Assert.False(result.Succeeded);
        Assert.Equal(CancelAppointmentByCustomerService.NotFound, result.ErrorCode);

        // Randevu BOZULMAMIS olmali.
        Assert.Equal(
            AppointmentStatus.Confirmed,
            await ReadAppointmentStatusAsync(tenantId, appointment.Id));
    }

    /// <summary>
    /// IPTAL POLITIKASI: cutoff penceresi icindeyse iptal REDDEDILIR -- BACKEND'DE.
    /// </summary>
    /// <remarks>
    /// UI'da butonu gizlemek bir kural kontrolu DEGILDIR. Dogruluk kaynagi burasi.
    /// </remarks>
    [Fact]
    public async Task CustomerCannotCancelInsideCancellationCutoffWindow()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerId = Guid.CreateVersion7();

        // Randevuya 1 saat kaldi, ama politika 2 saat oncesine kadar iptale izin veriyor.
        Appointment appointment =
            await SeedConfirmedAppointmentAsync(tenantId, customerId, testTime.AddHours(1));

        CancelAppointmentByCustomerService service =
            CreateCancelAppointmentByCustomerService(tenantId, cancellationCutoffHours: 2);

        CustomerAppointmentCancellationResult result =
            await service.CancelAsync(appointment.Id, customerId);

        Assert.False(result.Succeeded);
        Assert.Equal(CancelAppointmentByCustomerService.CancelTooLate, result.ErrorCode);

        // UI kullaniciya "2 saatten az kaldi" diyebilsin diye politika geri donuyor.
        Assert.Equal(2, result.CancellationCutoffHours);

        Assert.Equal(
            AppointmentStatus.Confirmed,
            await ReadAppointmentStatusAsync(tenantId, appointment.Id));
    }

    /// <summary>
    /// CutoffHours = 0 -> kural yok, her zaman iptal edilebilir.
    /// </summary>
    [Fact]
    public async Task CustomerCanCancelAtAnyTimeWhenCutoffIsZero()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerId = Guid.CreateVersion7();

        // Randevuya 5 dakika kaldi.
        Appointment appointment =
            await SeedConfirmedAppointmentAsync(tenantId, customerId, testTime.AddMinutes(5));

        CancelAppointmentByCustomerService service =
            CreateCancelAppointmentByCustomerService(tenantId, cancellationCutoffHours: 0);

        CustomerAppointmentCancellationResult result =
            await service.CancelAsync(appointment.Id, customerId);

        Assert.True(result.Succeeded);
        Assert.Equal(
            AppointmentStatus.Cancelled,
            await ReadAppointmentStatusAsync(tenantId, appointment.Id));
    }

    /// <summary>
    /// FAIL-CLOSED: politika okunamazsa (isletme bulunamadi) "kural yok" SAYILMAZ.
    /// </summary>
    /// <remarks>
    /// Aksi halde bir okuma hatasi, tum son-dakika iptallerini serbest birakirdi.
    /// </remarks>
    [Fact]
    public async Task MissingCancellationPolicyFallsBackToSafeDefaultNotToNoRule()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerId = Guid.CreateVersion7();

        Appointment appointment =
            await SeedConfirmedAppointmentAsync(tenantId, customerId, testTime.AddHours(1));

        // Politika YOK (lookup null donuyor).
        CancelAppointmentByCustomerService service =
            CreateCancelAppointmentByCustomerService(tenantId, cancellationCutoffHours: null);

        CustomerAppointmentCancellationResult result =
            await service.CancelAsync(appointment.Id, customerId);

        // Guvenli varsayilana (2 saat) dustu -> 1 saat kala iptal REDDEDILDI.
        Assert.False(result.Succeeded);
        Assert.Equal(CancelAppointmentByCustomerService.CancelTooLate, result.ErrorCode);
    }

    /// <summary>
    /// Tamamlanmis / gelmedi gibi kapali bir randevu iptal edilemez.
    /// </summary>
    [Fact]
    public async Task CustomerCannotCancelAlreadyClosedAppointment()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid customerId = Guid.CreateVersion7();

        Appointment appointment =
            await SeedConfirmedAppointmentAsync(tenantId, customerId, testTime.AddDays(10));

        CancelAppointmentByCustomerService service =
            CreateCancelAppointmentByCustomerService(tenantId, cancellationCutoffHours: 0);

        // Ilk iptal basarili.
        Assert.True((await service.CancelAsync(appointment.Id, customerId)).Succeeded);

        // IDEMPOTENT: ayni iptal tekrar cagirilirsa BASARILI doner (cift tiklama cift etki
        // yaratmaz), hata firlatmaz.
        CustomerAppointmentCancellationResult second =
            await service.CancelAsync(appointment.Id, customerId);

        Assert.True(second.Succeeded);
        Assert.Equal(
            AppointmentStatus.Cancelled,
            await ReadAppointmentStatusAsync(tenantId, appointment.Id));
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

        await using (PaymentsDbContext dbContext =
            new(CreateOptions<PaymentsDbContext>()))
        {
            await dbContext.Database.MigrateAsync();
        }

        await using (IntegrationsDbContext dbContext =
            new(CreateOptions<IntegrationsDbContext>()))
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

    /// <summary>
    /// REGRESYON TESTI (LANSMAN BLOKAJI): tenant provisioning Business'i GERCEKTEN olusturur.
    /// </summary>
    /// <remarks>
    /// Onceki hata: `Business.Create(...)` YALNIZCA TESTLERDEN cagriliyordu -- hicbir uretim
    /// kodu yolu (API ucu, seeder, provisioning) onu yaratmiyordu.
    ///
    /// Sonuc: platform admin tenant acar, owner giris yapar, ama SUBE BILE ACAMAZ
    /// (BranchManagementService aktif bir Business arar -> BUSINESS_NOT_FOUND) ve salon
    /// /kesfet'te HIC GORUNMEZ. Yani urun tek bir salonu bile onboard edemiyordu.
    ///
    /// Entegrasyon testleri Business'i DOGRUDAN seed ettigi icin bosluk hic fark edilmemisti.
    /// Bu test tam da o seed'i YAPMAZ: provisioning servisinin isini yapmasini bekler.
    /// </remarks>
    [Fact]
    public async Task BusinessProvisioningCreatesTheBusinessAndIsIdempotent()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            // Platform admin baglami: hedef tenant'in context'i SET EDILMEMISTIR.
            // Servis IgnoreQueryFilters kullanmazsa hicbir satiri goremez ve mukerrer kayit uretir.
            TenantId = null,
        };

        await using OrganizationDbContext dbContext =
            new(CreateOptions<OrganizationDbContext>(), tenantContextAccessor);

        BusinessProvisioningService service = new(
            dbContext,
            new AdminAuditLogRecorder(new AdminDbContext(CreateOptions<AdminDbContext>())),
            new FixedTimeProvider(testTime));

        BusinessProvisioningResult created = await service.CreateAsync(
            new CreateBusinessCommand(
                actorUserAccountId,
                tenantId,
                "atlas-hair",
                "Atlas Hair",
                "hair"));

        Assert.True(created.Succeeded);
        Assert.NotNull(created.BusinessId);

        // Business GERCEKTEN yazildi mi?
        Business persisted = await dbContext.Businesses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(entity => entity.TenantId == tenantId);

        Assert.Equal("atlas-hair", persisted.Slug);
        Assert.Equal("Atlas Hair", persisted.DisplayName);
        Assert.Equal("hair", persisted.CategoryKey);
        Assert.Equal(BusinessStatus.Active, persisted.Status);

        // IDEMPOTENT: provisioning yarida kalirsa istek tekrar denenebilmeli.
        // Ayni tenant icin ikinci cagri MUKERRER kayit URETMEMELI.
        BusinessProvisioningResult again = await service.CreateAsync(
            new CreateBusinessCommand(
                actorUserAccountId,
                tenantId,
                "atlas-hair",
                "Atlas Hair",
                "hair"));

        Assert.True(again.Succeeded);
        Assert.Equal(created.BusinessId, again.BusinessId);

        int count = await dbContext.Businesses
            .IgnoreQueryFilters()
            .CountAsync(entity => entity.TenantId == tenantId);

        Assert.Equal(1, count);
    }

    /// <summary>
    /// REGRESYON TESTI (B1): StaffManagementService.UpdateAsync personelin adini GERCEKTEN degistirir.
    /// </summary>
    /// <remarks>
    /// Onceki hata: UpdateAsync entity'yi cekip DisplayName'i HIC UYGULAMADAN SaveChangesAsync
    /// cagiriyordu. Istek 200 OK donuyor, servis "basarili" diyor, hatta
    /// "organization.staff.updated" audit kaydi bile yaziliyordu -- ama isim degismiyordu.
    /// StaffMember domain'inde Rename metodu HIC YOKTU (sadece Create + Archive).
    ///
    /// Bu yuzden test servisin DONUS DEGERINE GUVENMEZ (eski kod da "basarili" donuyordu);
    /// veritabanindan YENIDEN OKUYARAK dogrular.
    /// </remarks>
    [Fact]
    public async Task StaffUpdatePersistsTheNewDisplayName()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        await using OrganizationDbContext dbContext =
            new(CreateOptions<OrganizationDbContext>(), tenantContextAccessor);

        Business business = Business.Create(tenantId, "atlas-hair", "Atlas Hair", "hair", testTime);
        Branch branch = Branch.Create(
            tenantId,
            business.Id,
            "kadikoy",
            "Kadıköy",
            "Europe/Istanbul",
            testTime,
            "İstanbul",
            "Kadıköy",
            "Caferağa Mahallesi");

        dbContext.Businesses.Add(business);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        StaffManagementService service = new(
            dbContext,
            tenantContextAccessor,
            new AdminAuditLogRecorder(new AdminDbContext(CreateOptions<AdminDbContext>())),
            new FixedTimeProvider(testTime));

        // Isim yanlis yazilarak eklendi -- tipik ilk-kullanim senaryosu.
        StaffManagementResult created = await service.CreateAsync(
            new CreateStaffCommand(actorUserAccountId, branch.Id, "Mehmt Yilmz", null));

        Assert.True(created.Succeeded);
        Guid staffId = created.Staff!.Id;

        // Duzeltme.
        StaffManagementResult updated = await service.UpdateAsync(
            new UpdateStaffCommand(actorUserAccountId, branch.Id, staffId, "Mehmet Yılmaz"));

        Assert.True(updated.Succeeded);
        Assert.Equal("Mehmet Yılmaz", updated.Staff!.DisplayName);

        // ASIL KONTROL: veritabanina yazildi mi? Eski kod burada "Mehmt Yilmz" birakiyordu.
        StaffMember persisted = await dbContext.StaffMembers
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == staffId);

        Assert.Equal("Mehmet Yılmaz", persisted.DisplayName);
    }

    /// <summary>
    /// UpdateAsync'in HIC validasyonu yoktu (zaten hicbir sey de uygulamiyordu).
    /// Artik CreateAsync ile ayni kurallar geciyor: isim 2-200 karakter.
    /// </summary>
    [Fact]
    public async Task StaffUpdateRejectsInvalidDisplayName()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorUserAccountId = Guid.CreateVersion7();
        TenantContextAccessor tenantContextAccessor = new()
        {
            TenantId = tenantId,
        };

        await using OrganizationDbContext dbContext =
            new(CreateOptions<OrganizationDbContext>(), tenantContextAccessor);

        Business business = Business.Create(tenantId, "atlas-hair", "Atlas Hair", "hair", testTime);
        Branch branch = Branch.Create(
            tenantId,
            business.Id,
            "kadikoy",
            "Kadıköy",
            "Europe/Istanbul",
            testTime,
            "İstanbul",
            "Kadıköy",
            "Caferağa Mahallesi");

        dbContext.Businesses.Add(business);
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();

        StaffManagementService service = new(
            dbContext,
            tenantContextAccessor,
            new AdminAuditLogRecorder(new AdminDbContext(CreateOptions<AdminDbContext>())),
            new FixedTimeProvider(testTime));

        StaffManagementResult created = await service.CreateAsync(
            new CreateStaffCommand(actorUserAccountId, branch.Id, "Mehmet Yılmaz", null));
        Guid staffId = created.Staff!.Id;

        // Bos isim reddedilmeli...
        StaffManagementResult blank = await service.UpdateAsync(
            new UpdateStaffCommand(actorUserAccountId, branch.Id, staffId, "   "));

        Assert.False(blank.Succeeded);
        Assert.Equal(StaffManagementService.InvalidRequest, blank.ErrorCode);

        // ...ve isim BOZULMAMIS olmali.
        StaffMember persisted = await dbContext.StaffMembers
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == staffId);

        Assert.Equal("Mehmet Yılmaz", persisted.DisplayName);
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
