import { cn } from "@/shared/lib/cn";

const statusCopy: Record<string, string> = {
  Approved: "Onaylandı",
  Cancelled: "İptal edildi",
  CancelledByCustomer: "Müşteri iptal etti",
  Completed: "Tamamlandı",
  Confirmed: "Onaylandı",
  Declined: "Reddedildi",
  Expired: "Süresi doldu",
  NoShow: "Gelmedi",
  PendingApproval: "Onay bekliyor",
  Rebooked: "Yeniden planlandı",
  Superseded: "Başka talep seçildi"
};

const statusStyles: Record<string, string> = {
  Approved: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Cancelled: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  CancelledByCustomer: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  Completed: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Confirmed: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Declined: "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  Expired: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  NoShow: "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  PendingApproval:
    "border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] text-[var(--rs-warning)] shadow-[0_0_24px_rgb(217_119_6_/_0.12)]",
  Rebooked: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  Superseded: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)] line-through"
};

export function StatusBadge({
  className,
  status
}: {
  className?: string;
  status: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-2 rounded-full px-3 py-1 text-xs font-medium",
        statusStyles[status] ?? statusStyles.Cancelled,
        className
      )}
    >
      {status === "PendingApproval" ? (
        <span className="h-1.5 w-1.5 rounded-full bg-[var(--rs-warning)]" />
      ) : null}
      {statusCopy[status] ?? status}
    </span>
  );
}
