import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: { index: false },
  title: "Kaynaklara yönlendiriliyorsunuz"
};

// EKIPMAN TURLERI ARTIK /panel/kaynaklar SAYFASININ BIR SEKMESI (docs/29 birlestirme).
// Bu rota ESKI LINKLER + nav-manifest sozlesmesi icin duruyor: /panel/kaynaklar'a
// yonlendiriyor. Sayfa dosyasi silinmedi (routes.contract testi routes.resourceTypes
// icin bir page.tsx bekler; nav-manifest'te hidden kayitli).
export default function BusinessLegacyResourceTypesRoute() {
  redirect(routes.business.resources);
}
