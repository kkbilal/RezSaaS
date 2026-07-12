import type { Metadata } from "next";
import { CustomerShell } from "@/features/customer/components/customer-shell";
import { PrivateRouteState } from "@/features/session/components/private-route-state";
import { requireSession } from "@/features/session/lib/guards";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Profilim"
};

// Profil YAZILAMAZ: backend'de musteri profili guncelleme ucu YOK.
// Bu yuzden burada form YOK -- olmayan bir uca baglanan input, kullaniciyi kandiran
// bir dugmedir. Sadece dogrulanmis oturumdan okunan bilgiler gosterilir.
export default async function CustomerProfileRoute() {
  const sessionState = await requireSession(routes.customer.profile);

  if (sessionState.kind === "unavailable") {
    return (
      <PrivateRouteState
        actionHref={routes.auth.login}
        actionLabel="Giriş ekranına git"
        description={`${sessionState.reason} Lütfen yeniden giriş yapmayı dene.`}
        title="Oturum doğrulanamadı"
      />
    );
  }

  const account = sessionState.session.account;
  const email = account?.email ?? "Hesap";
  const displayName =
    typeof account === "object" && account && "displayName" in account
      ? String((account as { displayName?: unknown }).displayName ?? "")
      : "";

  return (
    <CustomerShell
      activeNav="profile"
      sessionDisplayName={displayName || undefined}
      sessionEmail={email}
    >
      <div className="space-y-6">
        <header className="space-y-2">
          <h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">
            Profil
          </h1>
          <p className="text-sm text-muted-foreground sm:text-base">
            Hesap bilgileriniz burada görünür.
          </p>
        </header>

        <Card>
          <CardHeader>
            <CardTitle>Hesap bilgileri</CardTitle>
            <CardDescription>
              Bilgiler doğrulanmış oturumunuzdan okunur.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <dl className="space-y-4 text-sm">
              <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
                <dt className="text-muted-foreground">E-posta</dt>
                <dd className="font-medium break-all">{email}</dd>
              </div>
              <Separator />
              <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
                <dt className="text-muted-foreground">Görünen ad</dt>
                <dd className="font-medium">{displayName || "Belirtilmedi"}</dd>
              </div>
            </dl>
          </CardContent>
        </Card>

        <Alert>
          <AlertTitle>Profil düzenleme henüz açık değil</AlertTitle>
          <AlertDescription>
            Ad, telefon ve hesap kapatma işlemleri ilgili servisler hazır olduğunda
            bu sayfada açılacak. Bilgilerinizi değiştirmek için şimdilik salonla
            iletişime geçebilirsiniz.
          </AlertDescription>
        </Alert>
      </div>
    </CustomerShell>
  );
}
