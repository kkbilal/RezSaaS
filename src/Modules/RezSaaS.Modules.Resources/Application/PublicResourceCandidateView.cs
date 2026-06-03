namespace RezSaaS.Modules.Resources.Application;

public sealed record PublicResourceCandidateView(
    Guid Id,
    Guid ResourceTypeId,
    string DisplayName);
