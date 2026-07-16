"use client";

import {
  getOperationDetails,
  operationIsDestructive,
  operationNeedsTimeRange,
  type AppointmentOperationKind
} from "@/features/business/api/business-appointment-operations";
import type { BusinessAppointment } from "@/features/business/api/get-business-appointments";
import { getServiceSummary, formatWindow } from "@/features/business/lib/appointment-format";
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle
} from "@/components/ui/sheet";
import { Textarea } from "@/components/ui/textarea";
import { createWebIdempotencyKey } from "@/shared/lib/idempotency";
import {
  formatBranchDateLabel,
  formatBranchTimeLabel,
  parseBranchDateTimeLocalValue,
  toBranchDateTimeLocalValue
} from "@/shared/lib/date-time";

/**
 * TEK randevu operasyon YUZEYI -- liste ve takvim ekranlari AYNI diyalogu kullanir.
 *
 * Operasyonun cagri mantigi business-appointment-operations.ts'te; bu dosya YALNIZCA
 * sunumdur (metin kutusu, saat kutulari, onay). Iki ekranda ayri yazilsaydi biri
 * duzeltilip digeri unutulurdu (or. yeniden planlama onizlemesi). Bu yuzden ortak.
 */

export type OperationDraft = {
  appointment: BusinessAppointment;
  endLocalValue: string;
  idempotencyKey: string;
  kind: AppointmentOperationKind;
  startLocalValue: string;
  text: string;
};

/**
 * Ekranin sectigi randevu + operasyon icin taslak uretir.
 * Yeniden planlama kutulari SUBE saatiyle doldurulur, tarayici saatiyle DEGIL.
 */
export function createOperationDraft(
  appointment: BusinessAppointment,
  kind: AppointmentOperationKind
): OperationDraft {
  return {
    appointment,
    endLocalValue: toBranchDateTimeLocalValue(
      appointment.endUtc,
      appointment.branchTimeZoneId
    ),
    idempotencyKey: createWebIdempotencyKey(`appointment-${kind}`),
    kind,
    startLocalValue: toBranchDateTimeLocalValue(
      appointment.startUtc,
      appointment.branchTimeZoneId
    ),
    text: kind === "note" ? appointment.businessNote ?? "" : ""
  };
}

/**
 * Operasyon yuzeyi.
 *
 * - YIKICI (iptal / gelmedi): AlertDialog -- geri alinamaz, onay ister.
 * - Digerleri: masaustunde Dialog, mobilde alttan Sheet (basparmakla ulasilabilir).
 */
export function OperationSurface({
  draft,
  isMobile,
  isSubmitting,
  onClose,
  onDraftChange,
  onSubmit
}: {
  draft: OperationDraft;
  isMobile: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onDraftChange: (patch: Partial<OperationDraft>) => void;
  onSubmit: () => void;
}) {
  const details = getOperationDetails(draft.kind);
  const description = `${getServiceSummary(draft.appointment)} · ${formatWindow(
    draft.appointment
  )}`;

  const body = (
    <div className="space-y-4">
      {operationNeedsTimeRange(draft.kind) ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="appointment-start">Başlangıç (şube saati)</Label>
            <input
              className="flex min-h-11 w-full rounded-md border bg-background px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
              id="appointment-start"
              onChange={(event) =>
                onDraftChange({ startLocalValue: event.target.value })
              }
              type="datetime-local"
              value={draft.startLocalValue}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="appointment-end">Bitiş (şube saati)</Label>
            <input
              className="flex min-h-11 w-full rounded-md border bg-background px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
              id="appointment-end"
              onChange={(event) =>
                onDraftChange({ endLocalValue: event.target.value })
              }
              type="datetime-local"
              value={draft.endLocalValue}
            />
          </div>
          {/* Girilen saatin hangi sube saatine denk geldigi GORUNUR yazilir. */}
          <p className="rounded-md bg-muted px-3 py-2 text-xs sm:col-span-2">
            Şube saati önizleme:{" "}
            <span className="font-medium">{formatDraftPreview(draft)}</span>
          </p>
        </div>
      ) : null}

      <div className="space-y-2">
        <Label htmlFor="appointment-text">{details.textareaLabel}</Label>
        <Textarea
          id="appointment-text"
          maxLength={details.maxLength}
          onChange={(event) => onDraftChange({ text: event.target.value })}
          placeholder={details.placeholder}
          rows={4}
          value={draft.text}
        />
        <p className="text-xs text-muted-foreground">{details.helper}</p>
      </div>
    </div>
  );

  if (operationIsDestructive(draft.kind)) {
    return (
      <AlertDialog onOpenChange={(open) => (open ? undefined : onClose())} open>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{details.title}</AlertDialogTitle>
            <AlertDialogDescription>
              {description} — bu işlem geri alınamaz.
            </AlertDialogDescription>
          </AlertDialogHeader>
          {body}
          <AlertDialogFooter>
            <AlertDialogCancel className="min-h-11" disabled={isSubmitting}>
              Vazgeç
            </AlertDialogCancel>
            <Button
              className="min-h-11"
              disabled={isSubmitting}
              onClick={onSubmit}
              variant="destructive"
            >
              {isSubmitting ? "İşleniyor" : details.submitLabel}
            </Button>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    );
  }

  const footer = (
    <>
      <Button
        className="min-h-11"
        disabled={isSubmitting}
        onClick={onClose}
        variant="outline"
      >
        Vazgeç
      </Button>
      <Button className="min-h-11" disabled={isSubmitting} onClick={onSubmit}>
        {isSubmitting ? "İşleniyor" : details.submitLabel}
      </Button>
    </>
  );

  if (isMobile) {
    return (
      <Sheet onOpenChange={(open) => (open ? undefined : onClose())} open>
        <SheetContent className="max-h-[90vh] overflow-y-auto" side="bottom">
          <SheetHeader>
            <SheetTitle>{details.title}</SheetTitle>
            <SheetDescription>{description}</SheetDescription>
          </SheetHeader>
          <div className="px-4">{body}</div>
          <SheetFooter>{footer}</SheetFooter>
        </SheetContent>
      </Sheet>
    );
  }

  return (
    <Dialog onOpenChange={(open) => (open ? undefined : onClose())} open>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{details.title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        {body}
        <DialogFooter>{footer}</DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

/** Yeniden planlama kutularinin girisini SUBE saatiyle geri okur (gorunur onizleme). */
function formatDraftPreview(draft: OperationDraft): string {
  const branchTimeZoneId = draft.appointment.branchTimeZoneId;
  const startUtc = parseBranchDateTimeLocalValue(
    draft.startLocalValue,
    branchTimeZoneId
  );
  const endUtc = parseBranchDateTimeLocalValue(
    draft.endLocalValue,
    branchTimeZoneId
  );

  if (!startUtc || !endUtc) {
    return "Geçerli şube zamanı gir.";
  }

  if (!branchTimeZoneId) {
    return `${startUtc} - ${endUtc} (UTC)`;
  }

  return `${formatBranchDateLabel(
    startUtc,
    branchTimeZoneId
  )} · ${formatBranchTimeLabel(startUtc, branchTimeZoneId)} - ${formatBranchTimeLabel(
    endUtc,
    branchTimeZoneId
  )}`;
}
