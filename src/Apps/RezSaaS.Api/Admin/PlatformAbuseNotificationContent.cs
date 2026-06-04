using RezSaaS.Modules.Admin.Application;
using RezSaaS.Modules.Admin.Domain;
using RezSaaS.Modules.Messaging.Application;
using RezSaaS.Modules.Messaging.Domain;

namespace RezSaaS.Api.Admin;

internal static class PlatformAbuseNotificationContent
{
    public static PlatformTransactionalMessageEnvelope CreateAppealDecision(
        AbuseAppealView appeal)
    {
        bool accepted = appeal.Status == AbuseAppealStatus.Accepted;

        return new PlatformTransactionalMessageEnvelope(
            appeal.UserAccountId,
            accepted
                ? PlatformMessagePurpose.AbuseAppealAccepted
                : PlatformMessagePurpose.AbuseAppealRejected,
            appeal.Id,
            $"abuse-appeal:{appeal.Id:D}:{appeal.Status}",
            accepted ? "RezSaaS itirazınız kabul edildi" : "RezSaaS itirazınız sonuçlandı",
            accepted
                ? "İtirazınız kabul edildi ve ilgili kayıt düzeltilmiştir."
                : "İtirazınız incelendi ve reddedilmiştir. Hesabınızdan güncel durumu görebilirsiniz.");
    }

    public static PlatformTransactionalMessageEnvelope CreateClosureProposal(
        AccountClosureCaseView closureCase)
    {
        return new PlatformTransactionalMessageEnvelope(
            closureCase.UserAccountId,
            PlatformMessagePurpose.AccountClosureProposed,
            closureCase.Id,
            $"account-closure-proposed:{closureCase.Id:D}",
            "RezSaaS hesap kapatma incelemesi",
            $"{closureCase.CustomerNotice} İtiraz süreniz bu e-posta teslim edildiğinde başlar. "
                + "RezSaaS hesabınıza giriş yaparak itiraz oluşturabilirsiniz.");
    }
}
