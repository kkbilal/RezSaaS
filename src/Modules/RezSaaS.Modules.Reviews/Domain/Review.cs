namespace RezSaaS.Modules.Reviews.Domain;

/// <summary>
/// Aggregate root for a customer review about a completed appointment.
/// Tenant-scoped; linked to a single completed appointment.
/// </summary>
public sealed class Review
{
    private Review()
    {
    }

    private Review(
        Guid id,
        Guid tenantId,
        Guid businessId,
        Guid branchId,
        Guid appointmentId,
        Guid customerUserAccountId,
        int rating,
        string comment,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        BusinessId = businessId;
        BranchId = branchId;
        AppointmentId = appointmentId;
        CustomerUserAccountId = customerUserAccountId;
        Rating = rating;
        Comment = comment;
        CreatedAtUtc = createdAtUtc;
        Status = ReviewStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid CustomerUserAccountId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public ReviewStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ModeratedAtUtc { get; private set; }
    public Guid? ModeratedByUserAccountId { get; private set; }
    public string? ModerationNote { get; private set; }

    /// <summary>
    /// Factory: creates a new review in <see cref="ReviewStatus.Pending"/> state.
    /// Validation of appointment completion status must be done by the application layer
    /// (cross-module contract with Booking module).
    /// </summary>
    public static Review Create(
        Guid tenantId,
        Guid businessId,
        Guid branchId,
        Guid appointmentId,
        Guid customerUserAccountId,
        int rating,
        string comment,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(businessId, nameof(businessId));
        RequireNonEmpty(branchId, nameof(branchId));
        RequireNonEmpty(appointmentId, nameof(appointmentId));
        RequireNonEmpty(customerUserAccountId, nameof(customerUserAccountId));

        if (rating is < 1 or > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        }

        string normalizedComment = (comment ?? string.Empty).Trim();
        if (normalizedComment.Length > 1_000)
        {
            throw new ArgumentException("Comment cannot exceed 1000 characters.", nameof(comment));
        }

        return new Review(
            Guid.CreateVersion7(),
            tenantId,
            businessId,
            branchId,
            appointmentId,
            customerUserAccountId,
            rating,
            normalizedComment,
            createdAtUtc);
    }

    /// <summary>
    /// Publishes the review: becomes visible on public profile and contributes to the rating summary.
    /// </summary>
    public void Publish(Guid moderatedByUserAccountId, DateTimeOffset moderatedAtUtc)
    {
        RequireNonEmpty(moderatedByUserAccountId, nameof(moderatedByUserAccountId));

        if (Status == ReviewStatus.Published)
        {
            return;
        }

        if (Status == ReviewStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected review cannot be published.");
        }

        Status = ReviewStatus.Published;
        ModeratedAtUtc = moderatedAtUtc;
        ModeratedByUserAccountId = moderatedByUserAccountId;
    }

    /// <summary>
    /// Rejects the review: not visible publicly and not counted in rating summary.
    /// </summary>
    public void Reject(
        Guid moderatedByUserAccountId,
        DateTimeOffset moderatedAtUtc,
        string? moderationNote = null)
    {
        RequireNonEmpty(moderatedByUserAccountId, nameof(moderatedByUserAccountId));

        if (Status == ReviewStatus.Rejected)
        {
            return;
        }

        if (Status == ReviewStatus.Published)
        {
            throw new InvalidOperationException("Published review cannot be rejected.");
        }

        string? normalizedNote = moderationNote?.Trim();
        if (normalizedNote is { Length: > 500 })
        {
            throw new ArgumentException("Moderation note cannot exceed 500 characters.", nameof(moderationNote));
        }

        Status = ReviewStatus.Rejected;
        ModeratedAtUtc = moderatedAtUtc;
        ModeratedByUserAccountId = moderatedByUserAccountId;
        ModerationNote = normalizedNote;
    }

    private static void RequireNonEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", paramName);
        }
    }
}