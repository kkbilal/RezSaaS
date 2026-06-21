namespace RezSaaS.Modules.Organization.Application;

public sealed record SkillManagementResult(
    bool Succeeded,
    string? ErrorCode,
    SkillView? Skill,
    IReadOnlyCollection<SkillView>? Skills)
{
    public static SkillManagementResult Success(SkillView skill)
    {
        return new SkillManagementResult(true, null, skill, null);
    }

    public static SkillManagementResult SuccessList(IReadOnlyCollection<SkillView> skills)
    {
        return new SkillManagementResult(true, null, null, skills);
    }

    public static SkillManagementResult Failure(string errorCode)
    {
        return new SkillManagementResult(false, errorCode, null, null);
    }
}
