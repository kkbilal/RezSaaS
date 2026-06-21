import { type ReactNode } from "react";
import { cn } from "@/shared/lib/cn";

export type EmptyStateProps = {
  icon?: ReactNode;
  title: ReactNode;
  description?: ReactNode;
  action?: ReactNode;
  className?: string;
};

export function EmptyState({
  icon,
  title,
  description,
  action,
  className
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center space-y-4 py-12 text-center",
        className
      )}
    >
      {icon && (
        <div className="flex h-16 w-16 items-center justify-center rounded-full bg-[var(--rs-surface-muted)] text-[var(--rs-accent)]">
          {icon}
        </div>
      )}
      <div className="space-y-2">
        <h3 className="text-lg font-semibold text-[var(--rs-ink)]">{title}</h3>
        {description && (
          <p className="text-sm text-[var(--rs-muted)]">{description}</p>
        )}
      </div>
      {action && action}
    </div>
  );
}