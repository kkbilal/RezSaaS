"use client";

import { useEffect, type ComponentPropsWithoutRef, type ReactNode } from "react";
import { cn } from "@/shared/lib/cn";

type DialogOverlayProps = {
  children: ReactNode;
  className?: string;
  onEscapeKeyDown?: () => void;
};

type DialogPanelProps = ComponentPropsWithoutRef<"section"> & {
  descriptionId?: string;
  titleId?: string;
};

type DialogFormPanelProps = ComponentPropsWithoutRef<"form"> & {
  descriptionId?: string;
  titleId?: string;
};

export function DialogOverlay({
  children,
  className,
  onEscapeKeyDown
}: DialogOverlayProps) {
  useEffect(() => {
    if (!onEscapeKeyDown) {
      return;
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onEscapeKeyDown?.();
      }
    }

    window.addEventListener("keydown", handleKeyDown);

    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onEscapeKeyDown]);

  return (
    <div
      className={cn(
        "fixed inset-0 z-40 grid place-items-center bg-[rgb(5_26_36_/_0.42)] p-4 backdrop-blur-sm",
        className
      )}
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
        "fade-up w-full max-w-2xl rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-6 shadow-[var(--rs-shadow-card)]",
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
  ...props
}: DialogFormPanelProps) {
  return (
    <form
      aria-describedby={descriptionId}
      aria-labelledby={titleId}
      aria-modal="true"
      className={cn(
        "fade-up w-full max-w-2xl rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] p-6 shadow-[var(--rs-shadow-card)]",
        className
      )}
      role="dialog"
      {...props}
    >
      {children}
    </form>
  );
}
