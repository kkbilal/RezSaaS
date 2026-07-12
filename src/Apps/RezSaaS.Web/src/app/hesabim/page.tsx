import { redirect } from "next/navigation";

import { routes } from "@/shared/config/routes";

// /hesabim'in kendi icerigi yok; musterinin inis noktasi randevu listesidir.
// (Bu rota customer-shell'de "Genel bakis" nav ogesiydi ama sayfasi YOKTU -> canli 404.)
export default function CustomerDashboardPage() {
  redirect(routes.customer.appointments);
}
