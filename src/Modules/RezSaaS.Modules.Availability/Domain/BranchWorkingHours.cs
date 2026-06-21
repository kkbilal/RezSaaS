namespace RezSaaS.Modules.Availability.Domain;

public sealed class BranchWorkingHours
{
    private BranchWorkingHours()
    {
    }

    private BranchWorkingHours(
        Guid id,
        Guid tenantId,
        Guid branchId,
        DayOfWeek dayOfWeek,
        TimeOnly opensAt,
        TimeOnly closesAt,
        bool isClosed)
    {
        RequireNonEmpty(tenantId, nameof(tenantId));
        RequireNonEmpty(branchId, nameof(branchId));

        if (!isClosed && closesAt <= opensAt)
        {
            throw new ArgumentException("Closing time must be later than opening time.", nameof(closesAt));
        }

        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        DayOfWeek = dayOfWeek;
        OpensAt = opensAt;
        ClosesAt = closesAt;
        IsClosed = isClosed;
    }

    public Guid BranchId { get; private set; }

    public TimeOnly ClosesAt { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public Guid Id { get; private set; }

    public bool IsClosed { get; private set; }

    public TimeOnly OpensAt { get; private set; }

    public Guid TenantId { get; private set; }

    public static BranchWorkingHours Create(
        Guid tenantId,
        Guid branchId,
        DayOfWeek dayOfWeek,
        TimeOnly opensAt,
        TimeOnly closesAt,
        bool isClosed = false)
    {
        return new BranchWorkingHours(
            Guid.CreateVersion7(),
            tenantId,
            branchId,
            dayOfWeek,
            opensAt,
            closesAt,
            isClosed);
    }

    public void SetHours(TimeOnly opensAt, TimeOnly closesAt, bool isClosed)
    {
        if (!isClosed && closesAt <= opensAt)
        {
            throw new ArgumentException("Closing time must be later than opening time.", nameof(closesAt));
        }

        OpensAt = opensAt;
        ClosesAt = closesAt;
        IsClosed = isClosed;
    }

    private static void RequireNonEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
