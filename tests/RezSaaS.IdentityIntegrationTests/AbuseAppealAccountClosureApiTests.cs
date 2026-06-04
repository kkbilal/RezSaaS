using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RezSaaS.Modules.Identity.Infrastructure.Security;
using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class AbuseAppealAccountClosureApiTests : IClassFixture<IdentityApiFixture>
{
    private const string Password = "RezSaaS!Auth1234";
    private readonly IdentityApiFixture fixture;

    public AbuseAppealAccountClosureApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CustomerCanAppealOwnBlockingSanctionWithoutSeeingInternalReason()
    {
        (Guid customerUserAccountId, string customerToken) =
            await RegisterAndLoginAsync();
        (_, string otherCustomerToken) = await RegisterAndLoginAsync();
        Guid adminUserAccountId = Guid.CreateVersion7();
        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(adminUserAccountId);
        const string internalReason = "Internal evidence: repeated automated slot spam.";
        HttpResponseMessage sanctionResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{customerUserAccountId}/sanctions",
            new
            {
                type = "Cooldown",
                reason = internalReason,
                endsAtUtc = DateTimeOffset.UtcNow.AddHours(4),
            });
        Assert.Equal(HttpStatusCode.Created, sanctionResponse.StatusCode);
        Guid sanctionId = await ReadGuidAsync(sanctionResponse, "sanctionId");

        HttpResponseMessage overviewResponse =
            await SendBearerAsync(
                HttpMethod.Get,
                "/api/customer/abuse/overview",
                customerToken);
        string overviewJson = await overviewResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, overviewResponse.StatusCode);
        Assert.DoesNotContain(internalReason, overviewJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"reason\"", overviewJson, StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage otherCustomerAppeal =
            await SendBearerAsync(
                HttpMethod.Post,
                "/api/customer/abuse/appeals",
                otherCustomerToken,
                new
                {
                    targetType = "UserSanction",
                    targetId = sanctionId,
                    statement = "Trying to appeal another account's sanction.",
                });
        Assert.Equal(HttpStatusCode.NotFound, otherCustomerAppeal.StatusCode);

        HttpResponseMessage appealResponse =
            await SendBearerAsync(
                HttpMethod.Post,
                "/api/customer/abuse/appeals",
                customerToken,
                new
                {
                    targetType = "UserSanction",
                    targetId = sanctionId,
                    statement = "This sanction is based on a false positive.",
                });
        Assert.Equal(HttpStatusCode.Created, appealResponse.StatusCode);
        Guid appealId = await ReadGuidAsync(appealResponse, "appealId");
        HttpResponseMessage appealDetailResponse =
            await SendBearerAsync(
                HttpMethod.Get,
                appealResponse.Headers.Location!.ToString(),
                customerToken);
        Assert.Equal(HttpStatusCode.OK, appealDetailResponse.StatusCode);
        HttpResponseMessage otherCustomerAppealDetailResponse =
            await SendBearerAsync(
                HttpMethod.Get,
                appealResponse.Headers.Location.ToString(),
                otherCustomerToken);
        Assert.Equal(HttpStatusCode.NotFound, otherCustomerAppealDetailResponse.StatusCode);

        HttpResponseMessage acceptResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/abuse/appeals/{appealId}/accept",
            new { reason = "Evidence review confirmed a false positive." });
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        HttpResponseMessage updatedOverview =
            await SendBearerAsync(
                HttpMethod.Get,
                "/api/customer/abuse/overview",
                customerToken);
        using JsonDocument overview =
            JsonDocument.Parse(await updatedOverview.Content.ReadAsStringAsync());
        JsonElement sanction = Assert.Single(
            overview.RootElement.GetProperty("sanctions").EnumerateArray());

        Assert.False(sanction.GetProperty("isActive").GetBoolean());
        Assert.Equal(
            "Accepted",
            Assert.Single(overview.RootElement.GetProperty("appeals").EnumerateArray())
                .GetProperty("status")
                .GetString());
    }

    [Fact]
    public async Task ClosureProposalRequiresSecondAdminAndAcceptedAppealCancelsIt()
    {
        (Guid customerUserAccountId, string customerToken) =
            await RegisterAndLoginAsync();
        await fixture.SeedHighRiskStrikesAsync(customerUserAccountId);
        Guid proposingAdminUserAccountId = Guid.CreateVersion7();
        Guid reviewingAdminUserAccountId = Guid.CreateVersion7();
        using HttpClient proposingAdminClient =
            fixture.CreatePlatformAdminStepUpClient(proposingAdminUserAccountId);
        using HttpClient reviewingAdminClient =
            fixture.CreatePlatformAdminStepUpClient(reviewingAdminUserAccountId);
        PublicBusinessProfileSeed tenantSeed = await fixture.SeedPublicBusinessProfileAsync();
        const string internalReason = "Internal high-risk evidence bundle.";
        const string customerNotice = "Your account is scheduled for closure after an appeal window.";
        HttpResponseMessage proposalResponse = await proposingAdminClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{customerUserAccountId}/closure-cases",
            new
            {
                internalReason,
                customerNotice,
            });
        Assert.Equal(HttpStatusCode.Created, proposalResponse.StatusCode);
        Guid closureCaseId = await ReadGuidAsync(proposalResponse, "closureCaseId");
        HttpResponseMessage closureDetailResponse = await reviewingAdminClient.GetAsync(
            proposalResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, closureDetailResponse.StatusCode);

        HttpResponseMessage proposalReplayResponse = await proposingAdminClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{customerUserAccountId}/closure-cases",
            new
            {
                internalReason,
                customerNotice,
            });
        Assert.Equal(HttpStatusCode.OK, proposalReplayResponse.StatusCode);
        Assert.Equal(
            closureCaseId,
            await ReadGuidAsync(proposalReplayResponse, "closureCaseId"));

        HttpResponseMessage membershipResponse = await reviewingAdminClient.PostAsJsonAsync(
            $"/api/admin/tenants/{tenantSeed.TenantId}/memberships",
            new
            {
                userAccountId = customerUserAccountId,
                role = "Staff",
                branchId = tenantSeed.BranchId,
            });
        Assert.Equal(HttpStatusCode.Conflict, membershipResponse.StatusCode);

        HttpResponseMessage selfApprovalResponse = await proposingAdminClient.PostAsJsonAsync(
            $"/api/admin/abuse/closure-cases/{closureCaseId}/approve",
            new { reason = "Self approval must be rejected." });
        Assert.Equal(HttpStatusCode.Conflict, selfApprovalResponse.StatusCode);

        HttpResponseMessage approvalResponse = await reviewingAdminClient.PostAsJsonAsync(
            $"/api/admin/abuse/closure-cases/{closureCaseId}/approve",
            new { reason = "Independent evidence review approved." });
        Assert.Equal(HttpStatusCode.OK, approvalResponse.StatusCode);

        HttpResponseMessage earlyExecutionResponse = await reviewingAdminClient.PostAsync(
            $"/api/admin/abuse/closure-cases/{closureCaseId}/execute",
            content: null);
        Assert.Equal(HttpStatusCode.Conflict, earlyExecutionResponse.StatusCode);

        HttpResponseMessage customerOverviewResponse =
            await SendBearerAsync(
                HttpMethod.Get,
                "/api/customer/abuse/overview",
                customerToken);
        string customerOverviewJson =
            await customerOverviewResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, customerOverviewResponse.StatusCode);
        Assert.Contains(customerNotice, customerOverviewJson, StringComparison.Ordinal);
        Assert.DoesNotContain(internalReason, customerOverviewJson, StringComparison.Ordinal);

        HttpResponseMessage appealResponse =
            await SendBearerAsync(
                HttpMethod.Post,
                "/api/customer/abuse/appeals",
                customerToken,
                new
                {
                    targetType = "AccountClosureCase",
                    targetId = closureCaseId,
                    statement = "The evidence attributes another person's activity to me.",
                });
        Assert.Equal(HttpStatusCode.Created, appealResponse.StatusCode);
        Guid appealId = await ReadGuidAsync(appealResponse, "appealId");

        HttpResponseMessage acceptedAppealResponse = await reviewingAdminClient.PostAsJsonAsync(
            $"/api/admin/abuse/appeals/{appealId}/accept",
            new { reason = "Identity attribution was incorrect." });
        Assert.Equal(HttpStatusCode.OK, acceptedAppealResponse.StatusCode);

        HttpResponseMessage cancelledCasesResponse = await reviewingAdminClient.GetAsync(
            $"/api/admin/abuse/closure-cases?userAccountId={customerUserAccountId}&status=CancelledByAppeal");
        using JsonDocument cancelledCases =
            JsonDocument.Parse(await cancelledCasesResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, cancelledCasesResponse.StatusCode);
        Assert.Single(cancelledCases.RootElement.GetProperty("closureCases").EnumerateArray());
    }

    [Fact]
    public async Task ActiveTenantMembershipBlocksAccountClosureProposal()
    {
        (Guid customerUserAccountId, _) = await RegisterAndLoginAsync();
        PublicBusinessProfileSeed tenantSeed = await fixture.SeedPublicBusinessProfileAsync();
        await fixture.GrantTenantMembershipAsync(
            tenantSeed.TenantId,
            customerUserAccountId,
            TenantMembershipRole.Staff,
            tenantSeed.BranchId);
        await fixture.SeedHighRiskStrikesAsync(customerUserAccountId);
        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());

        HttpResponseMessage proposalResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{customerUserAccountId}/closure-cases",
            new
            {
                internalReason = "High-risk evidence bundle.",
                customerNotice = "Your account is scheduled for closure.",
            });
        string responseJson = await proposalResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, proposalResponse.StatusCode);
        Assert.Contains(
            "ACCOUNT_CLOSURE_ACTIVE_TENANT_MEMBERSHIP",
            responseJson,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlatformRoleBlocksAccountClosureProposal()
    {
        string platformAdminEmail = $"closure-protected-admin-{Guid.NewGuid():N}@example.test";
        PlatformAdminBootstrapResult bootstrapResult =
            await fixture.BootstrapPlatformAdminAsync(
                platformAdminEmail,
                Password,
                "test-bootstrap-token");
        Assert.True(bootstrapResult.Succeeded);
        Guid protectedAdminUserAccountId =
            await fixture.GetUserAccountIdAsync(platformAdminEmail);
        await fixture.SeedHighRiskStrikesAsync(protectedAdminUserAccountId);
        using HttpClient operatorClient =
            fixture.CreatePlatformAdminStepUpClient(Guid.CreateVersion7());

        HttpResponseMessage proposalResponse = await operatorClient.PostAsJsonAsync(
            $"/api/admin/abuse/users/{protectedAdminUserAccountId}/closure-cases",
            new
            {
                internalReason = "High-risk evidence bundle.",
                customerNotice = "Your account is scheduled for closure.",
            });
        string responseJson = await proposalResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, proposalResponse.StatusCode);
        Assert.Contains(
            "ACCOUNT_CLOSURE_IDENTITY_INELIGIBLE",
            responseJson,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecutedClosureRejectsExistingBearerTokenAndCreatesPermanentSanction()
    {
        (Guid customerUserAccountId, string customerToken, string email) =
            await RegisterAndLoginWithEmailAsync();
        await fixture.SeedHighRiskStrikesAsync(customerUserAccountId);
        Guid proposingAdminUserAccountId = Guid.CreateVersion7();
        Guid reviewingAdminUserAccountId = Guid.CreateVersion7();
        Guid executingAdminUserAccountId = Guid.CreateVersion7();
        Guid closureCaseId = await fixture.SeedApprovedExecutableClosureCaseAsync(
            customerUserAccountId,
            proposingAdminUserAccountId,
            reviewingAdminUserAccountId);
        using HttpClient executingAdminClient =
            fixture.CreatePlatformAdminStepUpClient(executingAdminUserAccountId);

        HttpResponseMessage executionResponse = await executingAdminClient.PostAsync(
            $"/api/admin/abuse/closure-cases/{closureCaseId}/execute",
            content: null);
        Assert.Equal(HttpStatusCode.OK, executionResponse.StatusCode);

        HttpResponseMessage replayExecutionResponse = await executingAdminClient.PostAsync(
            $"/api/admin/abuse/closure-cases/{closureCaseId}/execute",
            content: null);
        Assert.Equal(HttpStatusCode.OK, replayExecutionResponse.StatusCode);

        HttpResponseMessage existingTokenResponse =
            await SendBearerAsync(
                HttpMethod.Get,
                "/api/customer/abuse/overview",
                customerToken);
        Assert.Equal(HttpStatusCode.Unauthorized, existingTokenResponse.StatusCode);

        HttpResponseMessage loginResponse = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);

        HttpResponseMessage adminOverviewResponse = await executingAdminClient.GetAsync(
            $"/api/admin/abuse/users/{customerUserAccountId}");
        string adminOverviewJson =
            await adminOverviewResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, adminOverviewResponse.StatusCode);
        Assert.Contains("PermanentClosure", adminOverviewJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClosureExecutionSafetyGateCanDisableAccountClosure()
    {
        (Guid customerUserAccountId, string customerToken) =
            await RegisterAndLoginAsync();
        await fixture.SeedHighRiskStrikesAsync(customerUserAccountId);
        Guid closureCaseId = await fixture.SeedApprovedExecutableClosureCaseAsync(
            customerUserAccountId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        using HttpClient adminClient =
            fixture.CreatePlatformAdminStepUpClient(
                Guid.CreateVersion7(),
                accountClosureExecutionEnabled: false);

        HttpResponseMessage executionResponse = await adminClient.PostAsync(
            $"/api/admin/abuse/closure-cases/{closureCaseId}/execute",
            content: null);
        string responseJson = await executionResponse.Content.ReadAsStringAsync();
        HttpResponseMessage existingTokenResponse =
            await SendBearerAsync(
                HttpMethod.Get,
                "/api/customer/abuse/overview",
                customerToken);

        Assert.Equal(HttpStatusCode.Conflict, executionResponse.StatusCode);
        Assert.Contains("ACCOUNT_CLOSURE_EXECUTION_DISABLED", responseJson, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, existingTokenResponse.StatusCode);
    }

    private async Task<(Guid UserAccountId, string Token)> RegisterAndLoginAsync()
    {
        (Guid userAccountId, string token, _) = await RegisterAndLoginWithEmailAsync();
        return (userAccountId, token);
    }

    private async Task<(Guid UserAccountId, string Token, string Email)> RegisterAndLoginWithEmailAsync()
    {
        string email = $"abuse-workflow-{Guid.NewGuid():N}@example.test";
        HttpResponseMessage registration = await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        Guid userAccountId = await fixture.GetUserAccountIdAsync(email);
        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using JsonDocument body =
            JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        string token = body.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token was not returned.");

        return (userAccountId, token, email);
    }

    private async Task<HttpResponseMessage> SendBearerAsync(
        HttpMethod method,
        string requestUri,
        string token,
        object? body = null)
    {
        using HttpRequestMessage request = new(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await fixture.Client.SendAsync(request);
    }

    private static async Task<Guid> ReadGuidAsync(
        HttpResponseMessage response,
        string propertyName)
    {
        using JsonDocument body =
            JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty(propertyName).GetGuid();
    }
}
