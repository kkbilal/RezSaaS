namespace RezSaaS.Modules.Integrations.Application;

public sealed record CreateWebhookSubscriptionCommand(
    Guid ActorUserAccountId,
    string DisplayName,
    string TargetUrl,
    IReadOnlyCollection<string> EventTypes);
