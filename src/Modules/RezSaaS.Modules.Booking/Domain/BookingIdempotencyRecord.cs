namespace RezSaaS.Modules.Booking.Domain;

public sealed class BookingIdempotencyRecord
{
    private BookingIdempotencyRecord()
    {
    }

    private BookingIdempotencyRecord(
        Guid id,
        Guid tenantId,
        Guid actorUserAccountId,
        string operation,
        string keyHash,
        string requestHash,
        Guid? responseResourceId,
        Guid? relatedResourceId,
        string responseStatus,
        int affectedRequests,
        DateTimeOffset? responseExpiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(actorUserAccountId, nameof(actorUserAccountId));

        Id = id;
        TenantId = tenantId;
        ActorUserAccountId = actorUserAccountId;
        Operation = NormalizeRequiredText(operation, nameof(operation));
        KeyHash = NormalizeRequiredText(keyHash, nameof(keyHash));
        RequestHash = NormalizeRequiredText(requestHash, nameof(requestHash));
        ResponseResourceId = responseResourceId;
        RelatedResourceId = relatedResourceId;
        ResponseStatus = NormalizeRequiredText(responseStatus, nameof(responseStatus));
        AffectedRequests = affectedRequests;
        ResponseExpiresAtUtc = responseExpiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid ActorUserAccountId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid Id { get; private set; }

    public string KeyHash { get; private set; } = string.Empty;

    public string Operation { get; private set; } = string.Empty;

    public string RequestHash { get; private set; } = string.Empty;

    public int AffectedRequests { get; private set; }

    public DateTimeOffset? ResponseExpiresAtUtc { get; private set; }

    public Guid? ResponseResourceId { get; private set; }

    public Guid? RelatedResourceId { get; private set; }

    public string ResponseStatus { get; private set; } = string.Empty;

    public Guid TenantId { get; private set; }

    public static BookingIdempotencyRecord Create(
        Guid tenantId,
        Guid actorUserAccountId,
        string operation,
        string keyHash,
        string requestHash,
        Guid? responseResourceId,
        Guid? relatedResourceId,
        string responseStatus,
        int affectedRequests,
        DateTimeOffset? responseExpiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        return new BookingIdempotencyRecord(
            Guid.CreateVersion7(),
            tenantId,
            actorUserAccountId,
            operation,
            keyHash,
            requestHash,
            responseResourceId,
            relatedResourceId,
            responseStatus,
            affectedRequests,
            responseExpiresAtUtc,
            createdAtUtc);
    }

    private static string NormalizeRequiredText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
