using RezSaaS.Api.Configuration;
using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Api.Admin;

public static class AdminOperationsEndpointExtensions
{
    public static IEndpointRouteBuilder MapAdminOperationsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder operations = endpoints
            .MapGroup("/api/admin/operations")
            .WithTags("Admin Operations")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        operations.MapGet("/reconciliation", GetReconciliationAsync);

        return endpoints;
    }

    private static async Task<IResult> GetReconciliationAsync(
        PlatformOperationsReconciliationService service,
        CancellationToken cancellationToken)
    {
        PlatformOperationsReconciliationSnapshot snapshot =
            await service.InspectAsync(cancellationToken);
        string status = snapshot.HasCriticalIssues
            ? "Critical"
            : snapshot.HasIssues
                ? "Degraded"
                : "Healthy";

        return Results.Ok(
            new AdminOperationsReconciliationResponse(
                snapshot.EvaluatedAtUtc,
                status,
                snapshot.Notifications.FailedCount,
                snapshot.Notifications.StaleProcessingCount,
                snapshot.Notifications.CallbackPendingCount,
                snapshot.AccountClosures.NotificationOverdueCount,
                snapshot.AccountClosures.ExecutionStalledCount,
                snapshot.Notifications.FailedMessageIds,
                snapshot.Notifications.StaleProcessingMessageIds,
                snapshot.Notifications.CallbackPendingMessageIds,
                snapshot.AccountClosures.NotificationOverdueClosureCaseIds,
                snapshot.AccountClosures.ExecutionStalledClosureCaseIds));
    }
}
