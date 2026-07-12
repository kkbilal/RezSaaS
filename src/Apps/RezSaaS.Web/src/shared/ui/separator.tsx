import type { ComponentPropsWithoutRef } from "react";
import { cn } from "@/shared/lib/cn";

type SeparatorProps = ComponentPropsWithoutRef<"hr"> & {
  orientation?: "horizontal" | "vertical";
  label?: string;
};

export function Separator({
  className,
  label,
  orientation = "horizontal",
  ...props
}: SeparatorProps) {
  if (orientation === "vertical") {
    return (
      <span
        aria-hidden
        className={cn("inline-block w-px self-stretch bg-[var(--rs-border)]", className)}
        role="separator"
        {...(props as object)}
      />
    );
  }

  if (label) {
    return (
      <span
        className={cn("flex items-center gap-3 text-xs uppercase tracking-[0.18em] text-[var(--rs-muted)]", className)}
        role="separator"
      >
        <span aria-hidden className="h-px flex-1 bg-[var(--rs-border)]" />
        <span>{label}</span>
        <span aria-hidden className="h-px flex-1 bg-[var(--rs-border)]" />
      </span>
    );
  }

  return (
    <hr
      className={cn("border-0 border-t border-[var(--rs-border)]", className)}
      {...props}
    />
  );
}
