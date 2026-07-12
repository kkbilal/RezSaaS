"use client";

import {
  useId,
  useState,
  type ReactNode
} from "react";
import { cn } from "@/shared/lib/cn";

type TooltipProps = {
  children: ReactNode;
  content: ReactNode;
  className?: string;
  side?: "top" | "bottom";
};

export function Tooltip({
  children,
  className,
  content,
  side = "top"
}: TooltipProps) {
  const [open, setOpen] = useState(false);
  const id = useId();

  return (
    <span
      className={cn("relative inline-flex", className)}
      onMouseEnter={() => setOpen(true)}
      onMouseLeave={() => setOpen(false)}
      onFocus={() => setOpen(true)}
      onBlur={() => setOpen(false)}
    >
      <span aria-describedby={open ? id : undefined}>{children}</span>
      <span
        id={id}
        role="tooltip"
        className={cn(
          "pointer-events-none absolute left-1/2 z-50 -translate-x-1/2 whitespace-nowrap rounded-full bg-[var(--rs-surface-strong)] px-3 py-1 text-xs font-medium text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] ring-1 ring-[var(--rs-border)] transition duration-150",
          side === "top" ? "bottom-full mb-2" : "top-full mt-2",
          open ? "opacity-100" : "opacity-0"
        )}
      >
        {content}
      </span>
    </span>
  );
}
