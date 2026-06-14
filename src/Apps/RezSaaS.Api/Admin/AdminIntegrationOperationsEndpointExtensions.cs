using RezSaaS.Api.Configuration;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Integrations.Application;

namespace RezSaaS.Api.Admin;

public static class AdminIntegrationOperationsEndpointExtensions
{
    public static IEndpointRouteBuilder MapAdminIntegrationOperationsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder integrations = endpoints
            .MapGroup("/api/admin/integrations")
            .WithTags("Admin Integrations")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        integrations.MapGet("/readiness", GetReadinessAsync)
            .Produces<AdminIntegrationReadinessResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status429TooManyRequests);

        return endpoints;
    }

    private static async Task<IResult> GetReadinessAsync(
        IntegrationReadinessService service,
        CancellationToken cancellationToken)
    {
        IntegrationReadinessSnapshot snapshot = await service.InspectAsync(cancellationToken);
        string status = snapshot.WebhookDeliveryEnabled
            ? "Ready"
            : snapshot.ExternalApiEnabled
                ? "Partial"
                : "Disabled";

        return Results.Ok(
            new AdminIntegrationReadinessResponse(
                snapshot.EvaluatedAtUtc,
                status,
                snapshot.ExternalApiEnabled,
                snapshot.WebhookDeliveryEnabled,
                snapshot.ApiKeyHashStorageReady,
                snapshot.WebhookSigningSecretHashReady,
                snapshot.StoresRawSecrets,
                snapshot.StoresRawWebhookPayloads,
                snapshot.ActiveApiClientCount,
                snapshot.ActiveWebhookSubscriptionCount,
                snapshot.PendingWebhookDeliveryCount,
                snapshot.FailedWebhookDeliveryCount));
    }
}
