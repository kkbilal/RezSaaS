"use client";

import { useEffect, type ComponentPropsWithoutRef, type ReactNode } from "react";
import { cn } from "@/shared/lib/cn";

type DialogOverlayProps = {
  children: ReactNode;
  className?: string;
  onEscapeKeyDown?: () => void;
  onClose?: () => void;
};

type DialogPanelProps = ComponentPropsWithoutRef<"section"> & {
  descriptionId?: string;
  titleId?: string;
};

type DialogFormPanelProps = Omit<ComponentPropsWithoutRef<"form">, "title"> & {
  descriptionId?: string;
  titleId?: string;
  title?: ReactNode;
  submitLabel?: string;
  loading?: boolean;
  onClose?: () => void;
};

export function DialogOverlay({
  children,
  className,
  onEscapeKeyDown,
  onClose
}: DialogOverlayProps) {
  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onEscapeKeyDown?.();
        onClose?.();
      }
    }

    if (onEscapeKeyDown || onClose) {
      window.addEventListener("keydown", handleKeyDown);
      return () => window.removeEventListener("keydown", handleKeyDown);
    }
  }, [onEscapeKeyDown, onClose]);

  return (
    <div
      className={cn(
        "fixed inset-0 z-40 grid place-items-center bg-black/70 p-4 backdrop-blur-md",
        className
      )}
      onClick={onClose}
    >
      {children}
    </div>
  );
}

export function DialogPanel({
  children,
  className,
  descriptionId,
  titleId,
  ...props
}: DialogPanelProps) {
  return (
    <section
      aria-describedby={descriptionId}
      aria-labelledby={titleId}
      aria-modal="true"
      className={cn(
        "fade-up w-full max-w-2xl rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl",
        className
      )}
      role="dialog"
      {...props}
    >
      {children}
    </section>
  );
}

export function DialogFormPanel({
  children,
  className,
  descriptionId,
  titleId,
  title,
  submitLabel = "Kaydet",
  loading = false,
  onClose,
  ...props
}: DialogFormPanelProps) {
  return (
    <form
      aria-describedby={descriptionId}
      aria-labelledby={titleId}
      aria-modal="true"
      className={cn(
        "fade-up w-full max-w-2xl rounded-[1.5rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-6 shadow-[var(--rs-shadow-card)] backdrop-blur-xl",
        className
      )}
      role="dialog"
      {...props}
    >
      {title && <h2 id={titleId} className="text-xl font-semibold text-[var(--rs-ink)]">{title}</h2>}
      <div className="mt-5">{children}</div>
      {(onClose || submitLabel) && (
        <div className="mt-6 flex justify-end gap-3">
          {onClose && (
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="rounded-full border border-[var(--rs-border)] bg-[var(--rs-glass)] px-5 py-2.5 text-sm font-medium text-[var(--rs-ink)] backdrop-blur-xl hover:-translate-y-0.5 hover:border-[var(--rs-border-strong)] disabled:pointer-events-none disabled:opacity-55"
            >
              İptal
            </button>
          )}
          {submitLabel && (
            <button
              type="submit"
              disabled={loading}
              className="rounded-full bg-[var(--rs-accent)] px-5 py-2.5 text-sm font-medium text-white shadow-[var(--rs-shadow-button)] hover:-translate-y-0.5 hover:bg-[var(--rs-accent-strong)] disabled:pointer-events-none disabled:opacity-55"
            >
              {loading ? "Kaydediliyor..." : submitLabel}
            </button>
          )}
        </div>
      )}
    </form>
  );
}