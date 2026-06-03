namespace RezSaaS.Modules.Organization.Application;

public sealed class PublicBusinessDirectoryOptions
{
    public const string SectionName = "Organization:PublicDiscovery";

    public int PermitLimit { get; init; } = 60;

    public int WindowMinutes { get; init; } = 1;

    public int DefaultTake { get; init; } = 20;

    public int MaxTake { get; init; } = 50;
}
