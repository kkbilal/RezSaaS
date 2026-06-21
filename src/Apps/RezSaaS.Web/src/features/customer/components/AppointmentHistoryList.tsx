import Link from "next/link";
import { formatBranchDateTime } from "@/shared/lib/date-time";
import { StatusBadge } from "@/shared/ui/status-badge";
import { Card } from "@/shared/ui/card";
import { Button } from "@/shared/ui/button";
import type { CustomerAppointmentHistoryItem } from "@/features/customer/api/get-appointment-history";

interface AppointmentHistoryListProps {
  items: CustomerAppointmentHistoryItem[];
}

export function AppointmentHistoryList({ items }: AppointmentHistoryListProps) {
  return (
    <div className="grid gap-4">
      {items.map((item, index) => (
        <AppointmentHistoryCard
          key={item.appointmentRequestId ?? item.appointmentId ?? index}
          item={item}
        />
      ))}
    </div>
  );
}

function AppointmentHistoryCard({ item }: { item: CustomerAppointmentHistoryItem }) {
  const status = item.status ?? "Unknown";
  const businessSlug = item.businessSlug;
  
  return (
    <Card className="p-5 hover:shadow-md transition-shadow">
      <div className="space-y-4">
        <div className="flex flex-wrap items-center gap-3">
          <StatusBadge status={status} />
          <span className="rounded-full border border-gray-200 bg-white px-3 py-1 text-xs text-gray-600">
            {getItemTypeCopy(item.itemType)}
          </span>
          <span className="text-sm text-gray-700 font-medium">
            {item.businessDisplayName ?? businessSlug}
          </span>
        </div>

        <div>
          <h3 className="text-xl font-semibold text-gray-900">
            {getLineSummary(item)}
          </h3>
          <p className="mt-1 text-sm text-gray-600">
            {item.branchDisplayName ?? "Şube"} ·{" "}
            {item.startUtc && item.branchTimeZoneId
              ? formatBranchDateTime(item.startUtc, item.branchTimeZoneId)
              : "Zaman bilgisi yok"}
          </p>
        </div>

        <div className="grid gap-3 md:grid-cols-3">
          <InfoBlock label="Personel" value={item.staffMemberDisplayName ?? "Atanacak"} />
          <InfoBlock
            label="Süre"
            value={`${getTotalDurationMinutes(item)} dk`}
          />
          <InfoBlock label="Toplam" value={formatTotalPrice(item)} />
        </div>

        {businessSlug && (
          <div className="pt-2">
            <Button asChild variant="secondary" className="w-full md:w-auto">
              <Link href={`/isletme/${businessSlug}`}>
                İşletme profili
              </Link>
            </Button>
          </div>
        )}
      </div>
    </Card>
  );
}

function InfoBlock({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-gray-200 bg-gray-50 p-3">
      <p className="text-xs text-gray-600">{label}</p>
      <p className="mt-1 font-medium text-gray-900">{value}</p>
    </div>
  );
}

function getItemTypeCopy(itemType?: string | null) {
  if (itemType === "AppointmentRequest") {
    return "Talep";
  }
  if (itemType === "Appointment") {
    return "Randevu";
  }
  return itemType ?? "Kayıt";
}

function getLineSummary(item: CustomerAppointmentHistoryItem) {
  const lines = item.lines ?? [];
  const firstLine = lines.at(0);
  const firstService = firstLine?.serviceNameSnapshot ?? "Hizmet detayı yok";

  if (lines.length <= 1) {
    return firstService;
  }

  return `${firstService} + ${lines.length - 1} hizmet`;
}

function getTotalDurationMinutes(item: CustomerAppointmentHistoryItem) {
  return (item.lines ?? []).reduce(
    (total, line) => total + (line.durationMinutes ?? 0),
    0
  );
}

function formatTotalPrice(item: CustomerAppointmentHistoryItem) {
  const lines = item.lines ?? [];
  const amount = lines.reduce((total, line) => total + (line.priceAmount ?? 0), 0);
  const currencyCode =
    lines.find((line) => line.currencyCode)?.currencyCode ?? "TRY";

  try {
    return new Intl.NumberFormat("tr-TR", {
      currency: currencyCode,
      maximumFractionDigits: 0,
      style: "currency"
    }).format(amount);
  } catch {
    return `${amount} ${currencyCode}`;
  }
}
