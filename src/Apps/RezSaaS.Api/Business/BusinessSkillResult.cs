namespace RezSaaS.Api.Business;

public sealed record BusinessSkillResult(
    BusinessSkillOutcome Outcome,
    string? ErrorCode,
    BusinessSkillResponse? Skill,
    IReadOnlyCollection<BusinessSkillResponse>? Skills)
{
    public static BusinessSkillResult Success(BusinessSkillResponse skill)
        => new(BusinessSkillOutcome.Success, null, skill, null);

    public static BusinessSkillResult SuccessList(IReadOnlyCollection<BusinessSkillResponse> skills)
        => new(BusinessSkillOutcome.Success, null, null, skills);

    public static BusinessSkillResult Failure(BusinessSkillOutcome outcome, string errorCode)
        => new(outcome, errorCode, null, null);
}
