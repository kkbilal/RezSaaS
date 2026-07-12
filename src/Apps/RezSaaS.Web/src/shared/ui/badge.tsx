import type { ComponentPropsWithoutRef } from "react";
import { cn } from "@/shared/lib/cn";

export type BadgeVariant =
  | "default"
  | "success"
  | "warning"
  | "danger"
  | "info"
  | "purple"
  | "orange"
  | "accent";

const variantStyles: Record<BadgeVariant, string> = {
  default:
    "bg-[var(--rs-glass)] text-[var(--rs-muted-strong)] border border-[var(--rs-border)]",
  success:
    "bg-[var(--rs-success-soft)] text-[var(--rs-success)] border border-[rgba(16,185,129,0.25)]",
  warning:
    "bg-[var(--rs-warning-soft)] text-[var(--rs-warning)] border border-[var(--rs-warning-border)]",
  danger:
    "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)] border border-[rgba(239,68,68,0.25)]",
  info:
    "bg-[var(--rs-accent-soft)] text-[var(--rs-accent-strong)] border border-[rgba(99,102,241,0.25)]",
  purple:
    "bg-[var(--rs-accent-violet-soft)] text-[var(--rs-accent-violet)] border border-[rgba(139,92,246,0.25)]",
  orange:
    "bg-[rgba(249,115,22,0.16)] text-[#fb923c] border border-[rgba(249,115,22,0.25)]",
  accent:
    "bg-[var(--rs-accent-soft)] text-[var(--rs-accent-strong)] border border-[rgba(99,102,241,0.25)]"
};

type BadgeProps = ComponentPropsWithoutRef<"span"> & {
  variant?: BadgeVariant;
};

export function Badge({
  children,
  className,
  variant = "default",
  ...props
}: BadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-md px-2 py-0.5 font-mono text-[11px] font-medium tracking-wide",
        variantStyles[variant],
        className
      )}
      {...props}
    >
      {children}
    </span>
  );
}
