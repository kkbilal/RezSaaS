namespace RezSaaS.Modules.Organization.Application;

public sealed record CreateSkillCommand(Guid ActorUserAccountId, string Name);
