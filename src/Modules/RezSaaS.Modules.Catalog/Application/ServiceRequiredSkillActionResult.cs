namespace RezSaaS.Modules.Catalog.Application;

public sealed record ServiceRequiredSkillActionResult(
    bool Succeeded,
    string? ErrorCode)
{
    public static ServiceRequiredSkillActionResult Success()
        => new(true, null);

    public static ServiceRequiredSkillActionResult Failure(string errorCode)
        => new(false, errorCode);
}
