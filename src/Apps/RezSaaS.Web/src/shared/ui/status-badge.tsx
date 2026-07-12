import { cn } from "@/shared/lib/cn";

const statusCopy: Record<string, string> = {
  Approved: "Onaylandı",
  Accepted: "Kabul edildi",
  Active: "Aktif",
  BranchManager: "Şube yöneticisi",
  BusinessOwner: "İşletme sahibi",
  Cancelled: "İptal edildi",
  CancelledByAppeal: "İtirazla kapandı",
  CancelledByCustomer: "Müşteri iptal etti",
  Closed: "Kapalı",
  Completed: "Tamamlandı",
  Confirmed: "Onaylandı",
  Critical: "Kritik",
  Degraded: "Bozulmuş",
  Declined: "Reddedildi",
  Executed: "Tamamlandı",
  Executing: "İşleniyor",
  Expired: "Süresi doldu",
  Healthy: "Sağlıklı",
  High: "Yüksek",
  Low: "Düşük",
  Medium: "Orta",
  NoShow: "Gelmedi",
  PendingApproval: "Onay bekliyor",
  PendingReview: "İncelemede",
  Rebooked: "Yeniden planlandı",
  Rejected: "Reddedildi",
  Revoked: "Geri alınmış",
  Staff: "Personel",
  Suspended: "Askıda",
  Superseded: "Başka talep seçildi"
};

const statusStyles: Record<string, string> = {
  Approved: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Accepted: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Active: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  BranchManager: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  BusinessOwner: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Cancelled: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  CancelledByAppeal: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  CancelledByCustomer: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  Closed: "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  Completed: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Confirmed: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Critical: "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  Degraded:
    "border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] text-[var(--rs-warning)]",
  Declined: "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  Executed: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  Executing:
    "border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] text-[var(--rs-warning)]",
  Expired: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  Healthy: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  High: "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  Low: "bg-[var(--rs-success-soft)] text-[var(--rs-success)]",
  Medium:
    "border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] text-[var(--rs-warning)]",
  NoShow: "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  PendingApproval:
    "border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] text-[var(--rs-warning)] shadow-[0_0_24px_rgb(217_119_6_/_0.12)]",
  PendingReview:
    "border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] text-[var(--rs-warning)]",
  Rebooked: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  Rejected: "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)]",
  Revoked: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  Staff: "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]",
  Suspended:
    "border border-[var(--rs-warning-border)] bg-[var(--rs-warning-soft)] text-[var(--rs-warning)]",
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
        "inline-flex items-center gap-2 rounded-full px-3 py-1 font-mono text-[11px] font-medium tracking-wide",
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
