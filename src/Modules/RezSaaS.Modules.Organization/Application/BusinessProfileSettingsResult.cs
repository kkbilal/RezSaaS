namespace RezSaaS.Modules.Organization.Application;

public sealed record BusinessProfileSettingsResult(
    bool Succeeded,
    string? ErrorCode,
    BusinessProfileSettingsView? Settings)
{
    public static BusinessProfileSettingsResult Success(BusinessProfileSettingsView settings)
    {
        return new BusinessProfileSettingsResult(true, null, settings);
    }

    public static BusinessProfileSettingsResult Failure(string errorCode)
    {
        return new BusinessProfileSettingsResult(false, errorCode, null);
    }
}
