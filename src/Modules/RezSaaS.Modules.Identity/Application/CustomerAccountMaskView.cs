namespace RezSaaS.Modules.Identity.Application;

public sealed record CustomerAccountMaskView(
    Guid UserAccountId,
    string MaskedEmail,
    string MaskedPhone);
