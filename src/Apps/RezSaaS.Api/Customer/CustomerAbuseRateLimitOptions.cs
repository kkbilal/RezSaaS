namespace RezSaaS.Api.Customer;

public sealed class CustomerAbuseRateLimitOptions
{
    public const string SectionName = "Admin:CustomerAbuseActions";

    public int PermitLimit { get; set; } = 20;

    public int WindowMinutes { get; set; } = 1;
}
