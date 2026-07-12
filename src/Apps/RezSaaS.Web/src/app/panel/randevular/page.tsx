import { redirect } from "next/navigation";

import { routes } from "@/shared/config/routes";

// GECICI: Randevu listesi bugun /panel (business-panel.tsx) icinde yasiyor.
// Takvim sayfasi bu rotaya link veriyordu ama sayfa YOKTU -> canli 404.
// Adim 2'de (Serit A) burasi gercek bir sayfa olacak: GET /api/business/appointments
// + 6 operasyon (cancel / complete / no-show / notes / rebook).
export default function BusinessAppointmentsPage() {
  redirect(routes.business.panel);
}
