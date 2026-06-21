using System.Security.Claims;
using RezSaaS.BuildingBlocks.Tenancy;
using RezSaaS.Modules.Organization.Application;
using RezSaaS.Modules.Reviews.Application;
using RezSaaS.Modules.TenantManagement.Application;

namespace RezSaaS.Api.Customer;

/// <summary>
/// Composes the customer "create review" flow. The customer is authenticated but
/// does NOT send a tenant header; tenant context is resolved from the verified
/// business slug (same pattern as public booking request creation) and restored
/// afterwards. The appointment must belong to the authenticated customer and be
/// completed in that tenant.
/// </summary>
public sealed class CustomerCreateReviewComposer
{
    private readonly PublicBusinessDirectoryService businessDirectoryService;
    private readonly TenantLifecycleQueryService tenantLifecycleQueryService;
    private readonly CreateReviewService createReviewService;
    private readonly ITenantContextAccessor tenantContextAccessor;

    public CustomerCreateReviewComposer(
        PublicBusinessDirectoryService businessDirectoryService,
        TenantLifecycleQueryService tenantLifecycleQueryService,
        CreateReviewService createReviewService,
        ITenantContextAccessor tenantContextAccessor)
    {
        this.businessDirectoryService = businessDirectoryService;
        this.tenantLifecycleQueryService = tenantLifecycleQueryService;
        this.createReviewService = createReviewService;
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public async Task<CustomerCreateReviewResult> CreateAsync(
        ClaimsPrincipal user,
        CustomerCreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserAccountId(user, out Guid customerUserAccountId))
        {
            return CustomerCreateReviewResult.Unauthorized();
        }

        if (request.Rating is < 1 or > 5)
        {
            return CustomerCreateReviewResult.BadRequest("CUSTOMER_REVIEW_INVALID_RATING");
        }

        string trimmedComment = (request.Comment ?? string.Empty).Trim();
        if (trimmedComment.Length is 0 or > 1_000)
        {
            return CustomerCreateReviewResult.BadRequest("CUSTOMER_REVIEW_INVALID_COMMENT");
        }

        PublicBusinessCompositionContext? business =
            await businessDirectoryService.GetCompositionContextBySlugAsync(
                request.BusinessSlug,
                cancellationToken);

        if (business is null
            || !await tenantLifecycleQueryService.IsActiveAsync(business.TenantId, cancellationToken))
        {
            return CustomerCreateReviewResult.NotFound();
        }

        Guid? previousTenantId = tenantContextAccessor.TenantId;
        tenantContextAccessor.TenantId = business.TenantId;

        try
        {
            ReviewOperationResult result = await createReviewService.CreateAsync(
                new CreateReviewCommand(
                    business.TenantId,
                    customerUserAccountId,
                    request.AppointmentId,
                    request.Rating,
                    trimmedComment),
                cancellationToken);

            if (!result.Succeeded)
            {
                return result.ErrorCode switch
                {
                    "REVIEW_APPOINTMENT_NOT_FOUND" => CustomerCreateReviewResult.NotFound(),
                    "REVIEW_APPOINTMENT_NOT_COMPLETED" =>
                        CustomerCreateReviewResult.Conflict("CUSTOMER_REVIEW_APPOINTMENT_NOT_COMPLETED"),
                    "REVIEW_DUPLICATE" => CustomerCreateReviewResult.Conflict("CUSTOMER_REVIEW_DUPLICATE"),
                    _ => CustomerCreateReviewResult.Failure(result.ErrorCode ?? "CUSTOMER_REVIEW_FAILED"),
                };
            }

            return CustomerCreateReviewResult.Success(result.Review!);
        }
        finally
        {
            tenantContextAccessor.TenantId = previousTenantId;
        }
    }

    private static bool TryGetUserAccountId(
        ClaimsPrincipal user,
        out Guid userAccountId)
    {
        string? rawUserId = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out userAccountId);
    }
}

public sealed record CustomerCreateReviewRequest(
    string BusinessSlug,
    Guid AppointmentId,
    int Rating,
    string Comment);

public sealed record CustomerCreateReviewResult(
    CustomerCreateReviewOutcome Outcome,
    string? ErrorCode,
    ReviewView? Review)
{
    public static CustomerCreateReviewResult Success(ReviewView review) =>
        new(CustomerCreateReviewOutcome.Success, null, review);

    public static CustomerCreateReviewResult Unauthorized() =>
        new(CustomerCreateReviewOutcome.Unauthorized, "CUSTOMER_REVIEW_UNAUTHORIZED", null);

    public static CustomerCreateReviewResult BadRequest(string code) =>
        new(CustomerCreateReviewOutcome.BadRequest, code, null);

    public static CustomerCreateReviewResult NotFound() =>
        new(CustomerCreateReviewOutcome.NotFound, "CUSTOMER_REVIEW_NOT_FOUND", null);

    public static CustomerCreateReviewResult Conflict(string code) =>
        new(CustomerCreateReviewOutcome.Conflict, code, null);

    public static CustomerCreateReviewResult Failure(string code) =>
        new(CustomerCreateReviewOutcome.Failure, code, null);
}

public enum CustomerCreateReviewOutcome
{
    Success,
    Unauthorized,
    BadRequest,
    NotFound,
    Conflict,
    Failure,
}

public sealed record CustomerCreateReviewResponse(
    Guid Id,
    Guid BusinessId,
    Guid BranchId,
    Guid AppointmentId,
    int Rating,
    string Comment,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModeratedAtUtc,
    IReadOnlyCollection<string> ServiceNames);