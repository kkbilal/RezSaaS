namespace RezSaaS.Modules.Admin.Application;

public sealed class AbuseRiskOptions
{
    public const string SectionName = "Admin:AbuseRisk";

    public int ElevatedStrikeThreshold { get; set; } = 2;

    public int HighStrikeThreshold { get; set; } = 3;

    public int MaxBusinessReportsPerActorPerDay { get; set; } = 20;

    public int StrikeLifetimeDays { get; set; } = 90;
}
