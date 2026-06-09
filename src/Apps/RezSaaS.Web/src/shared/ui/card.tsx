import type { ComponentPropsWithoutRef } from "react";
import { cn } from "@/shared/lib/cn";

export function Card({ className, ...props }: ComponentPropsWithoutRef<"section">) {
  return (
    <section
      className={cn(
        "rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] shadow-[var(--rs-shadow-card)]",
        className
      )}
      {...props}
    />
  );
}

export function CardHeader({ className, ...props }: ComponentPropsWithoutRef<"div">) {
  return <div className={cn("space-y-2", className)} {...props} />;
}

export function CardTitle({ className, ...props }: ComponentPropsWithoutRef<"h2">) {
  return (
    <h2
      className={cn(
        "text-xl font-semibold tracking-[-0.035em] text-[var(--rs-ink)]",
        className
      )}
      {...props}
    />
  );
}

export function CardDescription({
  className,
  ...props
}: ComponentPropsWithoutRef<"p">) {
  return (
    <p
      className={cn("text-sm leading-6 text-[var(--rs-muted)]", className)}
      {...props}
    />
  );
}
