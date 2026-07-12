import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Randevularıma yönlendiriliyorsunuz"
};

// ESKI LINKLER icin duruyor. Yon TERSINE cevrildi: gercek sayfa artik /hesabim/randevular.
// "Talep" musterinin kelimesi degil -- URL'de de gorunmemeli. Yer imlenmis/paylasilmis eski
// /hesabim/talepler linkleri kirilmasin diye sayfa dosyasi silinmedi, yonlendiriyor.
export default function CustomerLegacyRequestsRoute() {
  redirect(routes.customer.appointments);
}
