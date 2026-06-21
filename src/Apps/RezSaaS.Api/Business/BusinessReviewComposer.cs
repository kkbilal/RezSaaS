using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Reviews.Application;
using RezSaaS.Modules.Reviews.Domain;
using RezSaaS.Modules.TenantManagement.Application;
using ApiListResult = RezSaaS.Modules.Reviews.Application.BusinessReviewListResult;

namespace RezSaaS.Api.Business;

internal static class ReviewErrorCodes
{
    public const string Unauthorized = "BUSINESS_REVIEW_UNAUTHORIZED";
    public const string Forbidden = "BUSINESS_REVIEW_FORBIDDEN";
    public const string MissingTenantContext = "BUSINESS_REVIEW_MISSING_TENANT_CONTEXT";
}

/// <summary>
/// Composes business-panel review operations: list for moderation (any status)
/// and moderate (publish/reject). Tenant context is required via tenant header
/// and BusinessOwner/BranchManager membership is enforced.
/// </summary>
public sealed class BusinessReviewComposer
{
    private readonly BusinessEntityLabelQueryService businessEntityLabelQueryService;
    private readonly TenantBookingAuthorizationService authorizationService;
    private readonly BusinessReviewQueryService reviewQueryService;
    private readonly ModerateReviewService moderateReviewService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public BusinessReviewComposer(
        BusinessEntityLabelQueryService businessEntityLabelQueryService,
        TenantBookingAuthorizationService authorizationService,
        BusinessReviewQueryService reviewQueryService,
        ModerateReviewService moderateReviewService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.businessEntityLabelQueryService = businessEntityLabelQueryService;
        this.authorizationService = authorizationService;
        this.reviewQueryService = reviewQueryService;
        this.moderateReviewService = moderateReviewService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<BusinessReviewListResult> ListAsync(
        ClaimsPrincipal user,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessReviewListResult.Unauthorized();
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessReviewListResult.BadRequest(ReviewErrorCodes.MissingTenantContext);
        }

        if (!await authorizationService.CanManageBusinessSettingsAsync(
            tenantId,
            userAccountId,
            cancellationToken))
        {
            return BusinessReviewListResult.Forbidden(ReviewErrorCodes.Forbidden);
        }

        BusinessLabelView? business =
            await businessEntityLabelQueryService.GetBusinessLabelAsync(cancellationToken);

        if (business is null)
        {
            return BusinessReviewListResult.NotFound("BUSINESS_REVIEW_BUSINESS_NOT_FOUND");
        }

        ReviewStatus? statusFilter = ReviewStatusFilter.TryParse(status);

        ApiListResult serviceResult = await reviewQueryService.ListAsync(
            business.BusinessId,
            statusFilter,
            page,
            pageSize,
            cancellationToken);

        IReadOnlyCollection<BusinessReviewListItemResponse> items = serviceResult.Reviews
            .Select(review => new BusinessReviewListItemResponse(
                review.Id,
                review.AppointmentId,
                review.Rating,
                review.Comment,
                review.Status,
                review.CreatedAtUtc,
                review.ModeratedAtUtc,
                review.CustomerDisplayName,
                review.ServiceNames))
            .ToList();

        return BusinessReviewListResult.Success(
            serviceResult.TotalCount,
            serviceResult.Page,
            serviceResult.PageSize,
            items);
    }

    public async Task<BusinessReviewModerateResult> ModerateAsync(
        ClaimsPrincipal user,
        Guid reviewId,
        BusinessReviewModerateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid userAccountId))
        {
            return BusinessReviewModerateResult.Unauthorized();
        }

        if (tenantContextAccessor.TenantId is not { } tenantId)
        {
            return BusinessReviewModerateResult.BadRequest(ReviewErrorCodes.MissingTenantContext);
        }

        if (!await authorizationService.CanManageBusinessSettingsAsync(
            tenantId,
            userAccountId,
            cancellationToken))
        {
            return BusinessReviewModerateResult.Forbidden(ReviewErrorCodes.Forbidden);
        }

        string trimmedNote = (request.ModerationNote ?? string.Empty).Trim();

        ReviewOperationResult result = await moderateReviewService.ModerateAsync(
            new ModerateReviewCommand(
                tenantId,
                userAccountId,
                reviewId,
                request.Decision,
                string.IsNullOrEmpty(trimmedNote) ? null : trimmedNote),
            cancellationToken);

        if (!result.Succeeded)
        {
            return result.ErrorCode switch
            {
                "REVIEW_NOT_FOUND" => BusinessReviewModerateResult.NotFound(),
                "REVIEW_INVALID_DECISION" => BusinessReviewModerateResult.BadRequest("BUSINESS_REVIEW_INVALID_DECISION"),
                "REVIEW_INVALID_STATE" => BusinessReviewModerateResult.Conflict("BUSINESS_REVIEW_INVALID_STATE"),
                _ => BusinessReviewModerateResult.Failure(result.ErrorCode ?? "BUSINESS_REVIEW_MODERATE_FAILED"),
            };
        }

        ReviewView review = result.Review!;

        return BusinessReviewModerateResult.Success(new BusinessReviewResponse(
            review.Id,
            review.BusinessId,
            review.BranchId,
            review.AppointmentId,
            review.Rating,
            review.Comment,
            review.Status,
            review.CreatedAtUtc,
            review.ModeratedAtUtc,
            review.CustomerDisplayName,
            review.ServiceNames));
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(rawUserId, out userAccountId);
    }
}

public sealed record BusinessReviewModerateRequest(
    string Decision, // "publish" | "reject"
    string? ModerationNote);

public sealed record BusinessReviewListResult(
    BusinessReviewListOutcome Outcome,
    string? ErrorCode,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyCollection<BusinessReviewListItemResponse> Reviews)
{
    public static BusinessReviewListResult Success(
        int totalCount,
        int page,
        int pageSize,
        IReadOnlyCollection<BusinessReviewListItemResponse> reviews) =>
        new(BusinessReviewListOutcome.Success, null, totalCount, page, pageSize, reviews);

    public static BusinessReviewListResult Unauthorized() =>
        new(BusinessReviewListOutcome.Unauthorized, ReviewErrorCodes.Unauthorized, 0, 1, 20, Array.Empty<BusinessReviewListItemResponse>());

    public static BusinessReviewListResult BadRequest(string code) =>
        new(BusinessReviewListOutcome.BadRequest, code, 0, 1, 20, Array.Empty<BusinessReviewListItemResponse>());

    public static BusinessReviewListResult Forbidden(string code) =>
        new(BusinessReviewListOutcome.Forbidden, code, 0, 1, 20, Array.Empty<BusinessReviewListItemResponse>());

    public static BusinessReviewListResult NotFound(string code) =>
        new(BusinessReviewListOutcome.NotFound, code, 0, 1, 20, Array.Empty<BusinessReviewListItemResponse>());
}

public enum BusinessReviewListOutcome
{
    Success,
    Unauthorized,
    BadRequest,
    Forbidden,
    NotFound,
}

public sealed record BusinessReviewListItemResponse(
    Guid Id,
    Guid AppointmentId,
    int Rating,
    string Comment,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModeratedAtUtc,
    string CustomerDisplayName,
    IReadOnlyCollection<string> ServiceNames);

public sealed record BusinessReviewModerateResult(
    BusinessReviewModerateOutcome Outcome,
    string? ErrorCode,
    BusinessReviewResponse? Review)
{
    public static BusinessReviewModerateResult Success(BusinessReviewResponse review) =>
        new(BusinessReviewModerateOutcome.Success, null, review);

    public static BusinessReviewModerateResult Unauthorized() =>
        new(BusinessReviewModerateOutcome.Unauthorized, ReviewErrorCodes.Unauthorized, null);

    public static BusinessReviewModerateResult BadRequest(string code) =>
        new(BusinessReviewModerateOutcome.BadRequest, code, null);

    public static BusinessReviewModerateResult Forbidden(string code) =>
        new(BusinessReviewModerateOutcome.Forbidden, code, null);

    public static BusinessReviewModerateResult NotFound() =>
        new(BusinessReviewModerateOutcome.NotFound, "BUSINESS_REVIEW_NOT_FOUND", null);

    public static BusinessReviewModerateResult Conflict(string code) =>
        new(BusinessReviewModerateOutcome.Conflict, code, null);

    public static BusinessReviewModerateResult Failure(string code) =>
        new(BusinessReviewModerateOutcome.Failure, code, null);
}

public enum BusinessReviewModerateOutcome
{
    Success,
    Unauthorized,
    BadRequest,
    Forbidden,
    NotFound,
    Conflict,
    Failure,
}

public sealed record BusinessReviewResponse(
    Guid Id,
    Guid BusinessId,
    Guid BranchId,
    Guid AppointmentId,
    int Rating,
    string Comment,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModeratedAtUtc,
    string CustomerDisplayName,
    IReadOnlyCollection<string> ServiceNames);

internal static class ReviewStatusFilter
{
    public static ReviewStatus? TryParse(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return Enum.TryParse<ReviewStatus>(status, ignoreCase: true, out ReviewStatus parsed)
            ? parsed
            : null;
    }
}