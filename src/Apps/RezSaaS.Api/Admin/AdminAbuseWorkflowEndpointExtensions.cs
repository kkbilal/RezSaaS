using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Identity.Domain;

namespace RezSaaS.Api.Admin;

public static class AdminAbuseWorkflowEndpointExtensions
{
    public static IEndpointRouteBuilder MapAdminAbuseWorkflowEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder workflow = endpoints
            .MapGroup("/api/admin/abuse")
            .WithTags("Admin Abuse Workflow")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdminWithStepUp)
            .RequireRateLimiting(AdminControlPlaneRateLimitPolicyNames.Operations);

        workflow.MapGet("/appeals", GetAppealsAsync);
        workflow.MapGet("/appeals/{appealId:guid}", GetAppealAsync);
        workflow.MapGet("/closure-cases", GetClosureCasesAsync);
        workflow.MapGet("/closure-cases/{closureCaseId:guid}", GetClosureCaseAsync);
        workflow.MapPost("/appeals/{appealId:guid}/accept", AcceptAppealAsync);
        workflow.MapPost("/appeals/{appealId:guid}/reject", RejectAppealAsync);
        workflow.MapPost(
            "/users/{userAccountId:guid}/closure-cases",
            ProposeClosureAsync);
        workflow.MapPost(
            "/closure-cases/{closureCaseId:guid}/approve",
            ApproveClosureAsync);
        workflow.MapPost(
            "/closure-cases/{closureCaseId:guid}/reject",
            RejectClosureAsync);
        workflow.MapPost(
            "/closure-cases/{closureCaseId:guid}/execute",
            ExecuteClosureAsync);

        return endpoints;
    }

    private static Task<IResult> AcceptAppealAsync(
        Guid appealId,
        [FromBody] AdminReviewAbuseAppealRequest request,
        ClaimsPrincipal user,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        return ReviewAppealAsync(
            appealId,
            AbuseAppealStatus.Accepted,
            request,
            user,
            composer,
            cancellationToken);
    }

    private static Task<IResult> ApproveClosureAsync(
        Guid closureCaseId,
        [FromBody] AdminAccountClosureReviewRequest request,
        ClaimsPrincipal user,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        return ReviewClosureAsync(
            closureCaseId,
            AccountClosureCaseStatus.Approved,
            request,
            user,
            composer,
            cancellationToken);
    }

    private static async Task<IResult> ExecuteClosureAsync(
        Guid closureCaseId,
        ClaimsPrincipal user,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseWorkflowAccessResult result =
            await composer.ExecuteClosureAsync(closureCaseId, user, cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(result.ClosureCase)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetAppealsAsync(
        Guid? userAccountId,
        string? status,
        int? take,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseWorkflowAccessResult result =
            await composer.GetAppealsAsync(userAccountId, status, take, cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(new AdminAbuseAppealListResponse(result.Appeals))
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetAppealAsync(
        Guid appealId,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseWorkflowAccessResult result =
            await composer.GetAppealAsync(appealId, cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(result.Appeal)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetClosureCasesAsync(
        Guid? userAccountId,
        string? status,
        int? take,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseWorkflowAccessResult result =
            await composer.GetClosureCasesAsync(userAccountId, status, take, cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(new AdminAccountClosureCaseListResponse(result.ClosureCases))
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> GetClosureCaseAsync(
        Guid closureCaseId,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseWorkflowAccessResult result =
            await composer.GetClosureCaseAsync(closureCaseId, cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(result.ClosureCase)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> ProposeClosureAsync(
        Guid userAccountId,
        [FromBody] AdminAccountClosureProposalRequest request,
        ClaimsPrincipal user,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseWorkflowAccessResult result =
            await composer.ProposeClosureAsync(userAccountId, request, user, cancellationToken);

        if (result.Outcome == AdminAbuseOutcome.Created)
        {
            AdminAccountClosureCaseResponse response = result.ClosureCase!;

            return Results.Created(
                $"/api/admin/abuse/closure-cases/{response.ClosureCaseId}",
                response);
        }

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(result.ClosureCase)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static Task<IResult> RejectAppealAsync(
        Guid appealId,
        [FromBody] AdminReviewAbuseAppealRequest request,
        ClaimsPrincipal user,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        return ReviewAppealAsync(
            appealId,
            AbuseAppealStatus.Rejected,
            request,
            user,
            composer,
            cancellationToken);
    }

    private static Task<IResult> RejectClosureAsync(
        Guid closureCaseId,
        [FromBody] AdminAccountClosureReviewRequest request,
        ClaimsPrincipal user,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        return ReviewClosureAsync(
            closureCaseId,
            AccountClosureCaseStatus.Rejected,
            request,
            user,
            composer,
            cancellationToken);
    }

    private static async Task<IResult> ReviewAppealAsync(
        Guid appealId,
        AbuseAppealStatus decision,
        AdminReviewAbuseAppealRequest request,
        ClaimsPrincipal user,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseWorkflowAccessResult result =
            await composer.ReviewAppealAsync(
                appealId,
                decision,
                request,
                user,
                cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(result.Appeal)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static async Task<IResult> ReviewClosureAsync(
        Guid closureCaseId,
        AccountClosureCaseStatus decision,
        AdminAccountClosureReviewRequest request,
        ClaimsPrincipal user,
        AdminAbuseWorkflowComposer composer,
        CancellationToken cancellationToken)
    {
        AdminAbuseWorkflowAccessResult result =
            await composer.ReviewClosureAsync(
                closureCaseId,
                decision,
                request,
                user,
                cancellationToken);

        return result.Outcome == AdminAbuseOutcome.Success
            ? Results.Ok(result.ClosureCase)
            : ToErrorResult(result.Outcome, result.ErrorCode);
    }

    private static IResult ToErrorResult(
        AdminAbuseOutcome outcome,
        string? errorCode)
    {
        AdminControlPlaneErrorResponse error =
            new(errorCode ?? "ADMIN_ABUSE_WORKFLOW_FAILED");

        return outcome switch
        {
            AdminAbuseOutcome.BadRequest => Results.BadRequest(error),
            AdminAbuseOutcome.Unauthorized => Results.Unauthorized(),
            AdminAbuseOutcome.NotFound => Results.NotFound(error),
            AdminAbuseOutcome.Conflict => Results.Conflict(error),
            _ => Results.UnprocessableEntity(error),
        };
    }
}
