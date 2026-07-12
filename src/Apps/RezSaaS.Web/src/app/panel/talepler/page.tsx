import { redirect } from "next/navigation";

import { routes } from "@/shared/config/routes";

// GECICI: Talep kuyrugu bugun /panel (business-panel.tsx) icinde yasiyor.
// Sidebar'daki "Talepler" ogesi bu rotaya link veriyordu ama sayfa YOKTU -> canli 404.
// Adim 2'de (Serit A) burasi gercek bir sayfa olacak: GET /api/business/appointment-requests
// + RequestInboxCard, ve inbox /panel'den sokulecek.
export default function BusinessRequestsPage() {
  redirect(routes.business.panel);
}
