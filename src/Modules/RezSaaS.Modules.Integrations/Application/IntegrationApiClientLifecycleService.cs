using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Integrations.Domain;
using RezSaaS.Modules.Integrations.Infrastructure.Persistence;

namespace RezSaaS.Modules.Integrations.Application;

public sealed class IntegrationApiClientLifecycleService
{
    public const string MissingTenantContext = "MISSING_TENANT_CONTEXT";
    public const string NotFound = "INTEGRATION_API_CLIENT_NOT_FOUND";

    private readonly IntegrationsDbContext dbContext;
    private readonly IOptions<IntegrationReadinessOptions> options;
    private readonly ITenantContextAccessor tenantContextAccessor;
    private readonly TimeProvider timeProvider;

    public IntegrationApiClientLifecycleService(
        IntegrationsDbContext dbContext,
        IOptions<IntegrationReadinessOptions> options,
        ITenantContextAccessor tenantContextAccessor,
        TimeProvider timeProvider)
    {
        this.dbContext = dbContext;
        this.options = options;
        this.tenantContextAccessor = tenantContextAccessor;
        this.timeProvider = timeProvider;
    }

    public async Task<CreateIntegrationApiClientResult> CreateAsync(
        CreateIntegrationApiClientCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.ExternalApiEnabled)
        {
            throw new InvalidOperationException("External integration API is disabled.");
        }

        Guid tenantId = RequireTenantId();
        string scopeSet = NormalizeScopeSet(command.Scopes);
        IntegrationSecretMaterial secret = IntegrationSecretFactory.CreateApiKey();
        DateTimeOffset now = timeProvider.GetUtcNow();
        IntegrationApiClient client = IntegrationApiClient.Create(
            tenantId,
            command.DisplayName,
            secret.Prefix,
            secret.Sha256Hash,
            scopeSet,
            command.ActorUserAccountId,
            now);

        dbContext.ApiClients.Add(client);
        dbContext.AuditLogEntries.Add(
            IntegrationAuditLogEntry.Create(
                tenantId,
                command.ActorUserAccountId,
                "integrations.api_client.created",
                $$"""{"tenantId":"{{tenantId}}","apiClientId":"{{client.Id}}","keyPrefix":"{{client.KeyPrefix}}"}""",
                now));

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateIntegrationApiClientResult(
            client.Id,
            client.KeyPrefix,
            secret.Plaintext,
            now);
    }

    public async Task<IntegrationLifecycleResult> RevokeAsync(
        Guid apiClientId,
        Guid actorUserAccountId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return IntegrationLifecycleResult.Failure(MissingTenantContext);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        IntegrationApiClient? client = await LockApiClientAsync(
            tenantId,
            apiClientId,
            cancellationToken);

        if (client is null)
        {
            return IntegrationLifecycleResult.Failure(NotFound);
        }

        bool wasActive = client.Status == IntegrationApiClientStatus.Active;
        client.Revoke(actorUserAccountId, reason, now);

        if (wasActive)
        {
            dbContext.AuditLogEntries.Add(
                IntegrationAuditLogEntry.Create(
                    tenantId,
                    actorUserAccountId,
                    "integrations.api_client.revoked",
                    $$"""{"tenantId":"{{tenantId}}","apiClientId":"{{client.Id}}"}""",
                    now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return IntegrationLifecycleResult.Success(client.Id);
    }

    private async Task<IntegrationApiClient?> LockApiClientAsync(
        Guid tenantId,
        Guid apiClientId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ApiClients
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM integrations."IntegrationApiClients"
                WHERE "TenantId" = {tenantId}
                    AND "Id" = {apiClientId}
                FOR UPDATE
                """)
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken);
    }

    private Guid RequireTenantId()
    {
        return tenantContextAccessor.TenantId
            ?? throw new InvalidOperationException("Tenant context is required.");
    }

    private static string NormalizeScopeSet(IReadOnlyCollection<string> scopes)
    {
        string[] normalizedScopes = scopes
            .Select(scope => scope.Trim())
            .Where(scope => scope.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (normalizedScopes.Length == 0)
        {
            throw new ArgumentException("At least one API scope is required.", nameof(scopes));
        }

        return string.Join(' ', normalizedScopes);
    }
}
