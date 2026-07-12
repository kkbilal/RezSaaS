import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { routes } from "@/shared/config/routes";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Taleplerime yönlendiriliyorsunuz"
};

export default function CustomerLegacyAppointmentsRoute() {
  redirect(routes.customer.requests);
}
