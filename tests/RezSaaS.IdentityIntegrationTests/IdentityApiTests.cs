using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RezSaaS.Api.Business;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.TenantManagement.Domain;

namespace RezSaaS.IdentityIntegrationTests;

public sealed class IdentityApiTests : IClassFixture<IdentityApiFixture>
{
    private readonly IdentityApiFixture fixture;

    public IdentityApiTests(IdentityApiFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task SwaggerDocumentIsAvailableInDevelopment()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocumentIncludesFrontendBootstrapEndpoints()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement paths = body.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/session/bootstrap", out _));
        Assert.True(paths.TryGetProperty("/api/session/step-up", out _));
        Assert.True(paths.TryGetProperty("/api/business/context", out _));
        Assert.True(paths.TryGetProperty("/api/business/appointment-requests", out _));
        Assert.True(paths.TryGetProperty("/api/business/appointment-requests/pending", out _));
        Assert.True(paths.TryGetProperty("/api/business/appointment-requests/{appointmentRequestId}", out _));
        Assert.True(paths.TryGetProperty("/api/business/appointments", out _));
        Assert.True(paths.TryGetProperty("/api/business/appointments/{appointmentId}", out _));
        Assert.True(paths.TryGetProperty("/api/business/resources/{resourceId}/blocks", out _));
        Assert.True(paths.TryGetProperty("/api/customer/abuse/overview", out _));
        Assert.True(paths.TryGetProperty("/api/customer/abuse/appeals/{appealId}", out _));
        Assert.True(paths.TryGetProperty("/api/customer/abuse/appeals", out _));
        Assert.True(paths.TryGetProperty("/api/customer/appointment-history", out _));
        Assert.True(paths.TryGetProperty("/api/public/businesses", out _));
        Assert.True(paths.TryGetProperty("/api/public/businesses/{slug}", out _));
        Assert.True(paths.TryGetProperty("/api/public/businesses/{slug}/profile", out _));
        Assert.True(paths.TryGetProperty("/api/public/businesses/{slug}/slots", out _));
        Assert.True(paths.TryGetProperty("/api/public/businesses/{slug}/appointment-requests", out _));
        Assert.True(paths.TryGetProperty("/api/public/businesses/{slug}/appointment-requests/{appointmentRequestId}", out _));
        Assert.True(paths.TryGetProperty("/api/public/businesses/{slug}/appointment-requests/{appointmentRequestId}/cancel", out _));

        AssertOpenApiJsonResponse(
            paths,
            "/api/business/appointment-requests",
            "get",
            "200",
            "#/components/schemas/BusinessAppointmentRequestListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/business/appointment-requests/pending",
            "get",
            "200",
            "#/components/schemas/BusinessAppointmentRequestListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/business/appointment-requests/{appointmentRequestId}",
            "get",
            "200",
            "#/components/schemas/BusinessAppointmentRequestResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/business/appointment-requests/{appointmentRequestId}/approve",
            "post",
            "200",
            "#/components/schemas/BusinessAppointmentRequestDecisionResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/business/appointments",
            "get",
            "200",
            "#/components/schemas/BusinessAppointmentListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/business/appointments/{appointmentId}",
            "get",
            "200",
            "#/components/schemas/BusinessAppointmentResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/customer/abuse/overview",
            "get",
            "200",
            "#/components/schemas/CustomerAbuseOverviewResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/customer/abuse/appeals/{appealId}",
            "get",
            "200",
            "#/components/schemas/CustomerAbuseAppealResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/customer/abuse/appeals",
            "post",
            "201",
            "#/components/schemas/CustomerAbuseAppealResponse");
        AssertOpenApiJsonArrayResponse(
            paths,
            "/api/public/businesses",
            "get",
            "200",
            "#/components/schemas/PublicBusinessSummaryView");
        AssertOpenApiJsonResponse(
            paths,
            "/api/public/businesses/{slug}",
            "get",
            "200",
            "#/components/schemas/PublicBusinessProfileView");
        AssertOpenApiJsonResponse(
            paths,
            "/api/public/businesses/{slug}/profile",
            "get",
            "200",
            "#/components/schemas/PublicBusinessProfileResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/public/businesses/{slug}/slots",
            "get",
            "200",
            "#/components/schemas/PublicSlotSearchResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/public/businesses/{slug}/appointment-requests",
            "post",
            "201",
            "#/components/schemas/PublicAppointmentRequestCreateResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/public/businesses/{slug}/appointment-requests",
            "get",
            "200",
            "#/components/schemas/PublicAppointmentRequestListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/public/businesses/{slug}/appointment-requests/{appointmentRequestId}",
            "get",
            "200",
            "#/components/schemas/PublicAppointmentRequestResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/public/businesses/{slug}/appointment-requests/{appointmentRequestId}/cancel",
            "post",
            "200",
            "#/components/schemas/PublicAppointmentRequestResponse");
    }

    [Fact]
    public async Task SwaggerDocumentIncludesAdminControlPlaneTypedResponses()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement paths = body.RootElement.GetProperty("paths");

        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/bootstrap/platform-admin",
            "post",
            "200",
            "#/components/schemas/PlatformAdminBootstrapHttpResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/tenants",
            "post",
            "201",
            "#/components/schemas/AdminTenantProvisioningResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/tenants",
            "get",
            "200",
            "#/components/schemas/AdminTenantListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/tenants/{tenantId}",
            "get",
            "200",
            "#/components/schemas/AdminTenantDetailResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/tenants/{tenantId}/memberships",
            "get",
            "200",
            "#/components/schemas/AdminTenantMembershipListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/tenants/{tenantId}/memberships",
            "post",
            "201",
            "#/components/schemas/AdminTenantMembershipResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/events",
            "get",
            "200",
            "#/components/schemas/AdminAbuseEventListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/reports",
            "get",
            "200",
            "#/components/schemas/AdminAbuseReportListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/reports/{reportId}/confirm",
            "post",
            "200",
            "#/components/schemas/AdminAbuseReportReviewResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/users/{userAccountId}",
            "get",
            "200",
            "#/components/schemas/AdminUserAbuseOverviewResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/users/{userAccountId}/sanctions",
            "post",
            "201",
            "#/components/schemas/AdminUserSanctionResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/users/{userAccountId}/strikes/{strikeId}/revoke",
            "post",
            "200",
            "#/components/schemas/AdminUserStrikeResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/appeals",
            "get",
            "200",
            "#/components/schemas/AdminAbuseAppealListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/appeals/{appealId}",
            "get",
            "200",
            "#/components/schemas/AdminAbuseAppealResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/closure-cases",
            "get",
            "200",
            "#/components/schemas/AdminAccountClosureCaseListResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/closure-cases/{closureCaseId}",
            "get",
            "200",
            "#/components/schemas/AdminAccountClosureCaseResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/abuse/users/{userAccountId}/closure-cases",
            "post",
            "201",
            "#/components/schemas/AdminAccountClosureCaseResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/operations/reconciliation",
            "get",
            "200",
            "#/components/schemas/AdminOperationsReconciliationResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/integrations/readiness",
            "get",
            "200",
            "#/components/schemas/AdminIntegrationReadinessResponse");
        AssertOpenApiJsonResponse(
            paths,
            "/api/admin/payments/readiness",
            "get",
            "200",
            "#/components/schemas/AdminPaymentReadinessResponse");
    }

    [Fact]
    public async Task SessionBootstrapRequiresAuthenticatedUser()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/api/session/bootstrap");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SessionBootstrapReturnsAccountRolesStepUpAndMemberships()
    {
        Guid userAccountId = Guid.CreateVersion7();
        PublicBusinessProfileSeed seed = await fixture.SeedPublicBusinessProfileAsync();
        await fixture.GrantTenantMembershipAsync(
            seed.TenantId,
            userAccountId,
            TenantMembershipRole.BusinessOwner);
        using HttpClient client = fixture.CreatePlatformAdminStepUpClient(userAccountId);

        HttpResponseMessage response = await client.GetAsync("/api/session/bootstrap");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = body.RootElement;
        Assert.Equal(userAccountId, root.GetProperty("account").GetProperty("userAccountId").GetGuid());
        Assert.Contains(
            PlatformRoleNames.Administrator,
            root.GetProperty("platformRoles").EnumerateArray().Select(element => element.GetString() ?? string.Empty));
        Assert.True(root.GetProperty("stepUp").GetProperty("isSatisfied").GetBoolean());
        Assert.Equal(
            seed.TenantId,
            root.GetProperty("tenantMemberships")[0].GetProperty("tenantId").GetGuid());
        Assert.Equal(
            nameof(TenantMembershipRole.BusinessOwner),
            root.GetProperty("tenantMemberships")[0].GetProperty("role").GetString());
    }

    [Fact]
    public async Task BusinessContextRequiresAuthenticatedUser()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/api/business/context");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BusinessContextReturnsActiveTenantMembershipsAndCapabilities()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);
        string accessToken = await LoginWithBearerTokenAsync(email, password);
        Guid userAccountId = await fixture.GetUserAccountIdAsync(email);
        PublicBusinessProfileSeed seed = await fixture.SeedPublicBusinessProfileAsync();
        await fixture.GrantTenantMembershipAsync(
            seed.TenantId,
            userAccountId,
            TenantMembershipRole.BranchManager,
            seed.BranchId);
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/business/context");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement tenant = body.RootElement.GetProperty("tenants")[0];
        Assert.Equal(seed.TenantId, tenant.GetProperty("tenantId").GetGuid());
        Assert.Equal(seed.BranchId, tenant.GetProperty("branchId").GetGuid());
        Assert.Equal(nameof(TenantMembershipRole.BranchManager), tenant.GetProperty("role").GetString());
        Assert.False(tenant.GetProperty("isTenantWide").GetBoolean());
        Assert.Contains(
            BusinessCapabilityNames.ManageAppointmentRequests,
            tenant.GetProperty("capabilities").EnumerateArray().Select(element => element.GetString() ?? string.Empty));
    }

    [Fact]
    public async Task UnsafePostWithMismatchedOriginIsRejected()
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/register");
        request.Headers.Add("Origin", "https://evil.example");
        request.Content = JsonContent.Create(
            new
            {
                email = CreateEmail(),
                password = "RezSaaS!Auth1234",
            });

        HttpResponseMessage response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegistrationAndBearerLoginAllowProtectedAccountAccess()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";

        HttpResponseMessage registration = await RegisterAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        string accessToken = await LoginWithBearerTokenAsync(email, password);

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/auth/manage/info");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage accountInfo = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, accountInfo.StatusCode);

        using JsonDocument body =
            JsonDocument.Parse(await accountInfo.Content.ReadAsStringAsync());
        Assert.Equal(email, body.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task DefaultLoginDoesNotRequireOneTimeCode()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);

        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("accessToken", out JsonElement accessToken));
        Assert.False(string.IsNullOrWhiteSpace(accessToken.GetString()));
        Assert.False(body.RootElement.TryGetProperty("requiresTwoFactor", out _));
    }

    [Fact]
    public async Task InvalidPasswordIsRejected()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);

        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password = "Wrong!Password123" });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task SuspendedAccountCannotSignIn()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);
        await fixture.SuspendUserAsync(email);

        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task SuspendedAccountCannotUseExistingBearerToken()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);
        string accessToken = await LoginWithBearerTokenAsync(email, password);
        await fixture.SuspendUserAsync(email);
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/auth/manage/info");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage accountInfo = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, accountInfo.StatusCode);
    }

    [Fact]
    public async Task MigrationDoesNotProvisionPlatformRoles()
    {
        Assert.Equal(0, await fixture.GetPlatformRoleCountAsync());
    }

    [Fact]
    public async Task CookieLoginAllowsProtectedAccountAccess()
    {
        string email = CreateEmail();
        const string password = "RezSaaS!Auth1234";
        await RegisterAsync(email, password);

        using HttpClient cookieClient = fixture.CreateClient();
        HttpResponseMessage login = await cookieClient.PostAsJsonAsync(
            "/api/auth/login?useCookies=true",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        HttpResponseMessage accountInfo = await cookieClient.GetAsync("/api/auth/manage/info");
        Assert.Equal(HttpStatusCode.OK, accountInfo.StatusCode);
    }

    private async Task<HttpResponseMessage> RegisterAsync(string email, string password)
    {
        return await fixture.Client.PostAsJsonAsync(
            "/api/auth/register",
            new { email, password });
    }

    private async Task<string> LoginWithBearerTokenAsync(string email, string password)
    {
        HttpResponseMessage login = await fixture.Client.PostAsJsonAsync(
            "/api/auth/login?useCookies=false",
            new { email, password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using JsonDocument body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("The access token was not returned.");
    }

    private static string CreateEmail()
    {
        return $"identity-test-{Guid.NewGuid():N}@example.test";
    }

    private static void AssertOpenApiJsonResponse(
        JsonElement paths,
        string path,
        string method,
        string statusCode,
        string expectedSchemaReference)
    {
        JsonElement response = paths
            .GetProperty(path)
            .GetProperty(method)
            .GetProperty("responses")
            .GetProperty(statusCode);

        Assert.True(
            response.TryGetProperty("content", out JsonElement content),
            $"{method.ToUpperInvariant()} {path} {statusCode} response must declare typed content.");
        Assert.Equal(
            expectedSchemaReference,
            content
                .GetProperty("application/json")
                .GetProperty("schema")
                .GetProperty("$ref")
                .GetString());
    }

    private static void AssertOpenApiJsonArrayResponse(
        JsonElement paths,
        string path,
        string method,
        string statusCode,
        string expectedItemSchemaReference)
    {
        JsonElement response = paths
            .GetProperty(path)
            .GetProperty(method)
            .GetProperty("responses")
            .GetProperty(statusCode);

        Assert.True(
            response.TryGetProperty("content", out JsonElement content),
            $"{method.ToUpperInvariant()} {path} {statusCode} response must declare typed content.");

        JsonElement schema = content
            .GetProperty("application/json")
            .GetProperty("schema");
        Assert.Equal("array", schema.GetProperty("type").GetString());
        Assert.Equal(
            expectedItemSchemaReference,
            schema
                .GetProperty("items")
                .GetProperty("$ref")
                .GetString());
    }
}
