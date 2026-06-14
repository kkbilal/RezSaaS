using RezSaaS.Api.Configuration;
using RezSaaS.Modules.Identity.Domain;
using RezSaaS.Modules.Payments.Application;

namespace RezSaaS.Api.Admin;

public static class AdminPaymentOperationsEndpointExtensions
{
    public static IEndpointRouteBuilder MapAdminPaymentOperationsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder payments = endpoints
            .MapGroup("/api/admin/payments")
            .WithTags("Admin Payments")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        payments.MapGet("/readiness", GetReadinessAsync)
            .Produces<AdminPaymentReadinessResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status429TooManyRequests);

        return endpoints;
    }

    private static async Task<IResult> GetReadinessAsync(
        PaymentReadinessService service,
        CancellationToken cancellationToken)
    {
        PaymentReadinessSnapshot snapshot = await service.InspectAsync(cancellationToken);
        string status = snapshot.OnlineCollectionEnabled && !snapshot.ProviderConfigured
            ? "Blocked"
            : snapshot.OnlineCollectionEnabled
                ? "Ready"
                : "Disabled";

        return Results.Ok(
            new AdminPaymentReadinessResponse(
                snapshot.EvaluatedAtUtc,
                status,
                snapshot.OnlineCollectionEnabled,
                snapshot.ProviderConfigured,
                snapshot.HostedCheckoutOnly,
                snapshot.StoresRawCardData,
                snapshot.WebhookIdempotencyStorageReady,
                snapshot.PolicyCount,
                snapshot.OpenIntentCount,
                snapshot.UnprocessedWebhookEventCount));
    }
}
