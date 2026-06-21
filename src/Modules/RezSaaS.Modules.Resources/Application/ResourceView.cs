namespace RezSaaS.Modules.Resources.Application;

public sealed record ResourceView(Guid Id, Guid ResourceTypeId, string DisplayName, string Status);
