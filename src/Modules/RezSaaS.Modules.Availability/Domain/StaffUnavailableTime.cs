namespace RezSaaS.Modules.Availability.Domain;

public sealed class StaffUnavailableTime
{
    private StaffUnavailableTime()
    {
    }

    private StaffUnavailableTime(
        Guid id,
        Guid tenantId,
        Guid staffMemberId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string reason)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(staffMemberId, nameof(staffMemberId));

        if (endUtc <= startUtc)
        {
            throw new ArgumentException("End must be later than start.", nameof(endUtc));
        }

        Id = id;
        TenantId = tenantId;
        StaffMemberId = staffMemberId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Reason = string.IsNullOrWhiteSpace(reason) ? "Unavailable" : reason.Trim();
    }

    public DateTimeOffset EndUtc { get; private set; }

    public Guid Id { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public Guid StaffMemberId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public Guid TenantId { get; private set; }

    public static StaffUnavailableTime Create(
        Guid tenantId,
        Guid staffMemberId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string reason)
    {
        return new StaffUnavailableTime(
            Guid.CreateVersion7(),
            tenantId,
            staffMemberId,
            startUtc,
            endUtc,
            reason);
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
