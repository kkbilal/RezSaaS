"use client";

import { useId, type ReactNode } from "react";
import { cn } from "@/shared/lib/cn";

export type TabItem<T extends string> = {
  value: T;
  label: ReactNode;
  badge?: ReactNode;
  disabled?: boolean;
};

type TabsProps<T extends string> = {
  items: ReadonlyArray<TabItem<T>>;
  value: T;
  onChange: (value: T) => void;
  className?: string;
  size?: "sm" | "md";
  id?: string;
};

export function Tabs<T extends string>({
  className,
  id,
  items,
  onChange,
  size = "md",
  value
}: TabsProps<T>) {
  const generatedId = useId();
  const tablistId = id ?? generatedId;

  return (
    <div
      className={cn(
        "inline-flex flex-wrap gap-1 rounded-full border border-[var(--rs-border)] bg-[var(--rs-surface-muted)] p-1",
        className
      )}
      role="tablist"
      id={tablistId}
    >
      {items.map((item) => {
        const selected = item.value === value;
        return (
          <button
            key={item.value}
            type="button"
            role="tab"
            id={`${tablistId}-tab-${item.value}`}
            aria-selected={selected}
            aria-controls={`${tablistId}-panel-${item.value}`}
            disabled={item.disabled}
            onClick={() => onChange(item.value)}
              className={cn(
                "inline-flex items-center gap-2 rounded-full font-medium transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--rs-focus)] disabled:pointer-events-none disabled:opacity-50",
                size === "sm" ? "px-3 py-1 text-xs" : "px-4 py-1.5 text-sm",
                selected
                  ? "bg-[var(--rs-glass-strong)] text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] backdrop-blur-xl"
                  : "text-[var(--rs-muted)] hover:text-[var(--rs-ink)]"
              )}
          >
            <span>{item.label}</span>
            {item.badge !== undefined && item.badge !== null ? (
              <span
                className={cn(
                  "inline-flex min-w-5 items-center justify-center rounded-full px-1.5 text-[0.65rem] font-semibold",
                  selected
                    ? "bg-[var(--rs-accent)] text-white"
                    : "bg-[var(--rs-neutral-soft)] text-[var(--rs-muted)]"
                )}
              >
                {item.badge}
              </span>
            ) : null}
          </button>
        );
      })}
    </div>
  );
}
