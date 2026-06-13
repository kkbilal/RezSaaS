namespace RezSaaS.Api.Business;

public sealed record BusinessSettingsResult(
    BusinessSettingsOutcome Outcome,
    string? ErrorCode,
    BusinessProfileSettingsResponse? Profile)
{
    public static BusinessSettingsResult Success(BusinessProfileSettingsResponse profile)
    {
        return new BusinessSettingsResult(BusinessSettingsOutcome.Success, null, profile);
    }

    public static BusinessSettingsResult Failure(
        BusinessSettingsOutcome outcome,
        string errorCode)
    {
        return new BusinessSettingsResult(outcome, errorCode, null);
    }
}
