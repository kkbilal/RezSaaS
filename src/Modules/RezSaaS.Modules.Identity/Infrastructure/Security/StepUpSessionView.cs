namespace RezSaaS.Modules.Identity.Infrastructure.Security;

public sealed record StepUpSessionView(
    Guid Id,
    Guid UserAccountId,
    string Method,
    DateTimeOffset ExpiresAtUtc);
