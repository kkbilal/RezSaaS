namespace RezSaaS.Api.Business;

public sealed record BusinessSkillCreateRequest(string Name);

public sealed record BusinessStaffSkillAssignRequest(Guid SkillId);
