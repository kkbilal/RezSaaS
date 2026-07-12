import type { ComponentPropsWithoutRef } from "react";
import { cn } from "@/shared/lib/cn";

type CardProps = ComponentPropsWithoutRef<"section"> & {
  interactive?: boolean;
};

export function Card({
  className,
  interactive = false,
  ...props
}: CardProps) {
  return (
    <section
      className={cn(
        "rounded-[1.25rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] shadow-[var(--rs-shadow-card)] backdrop-blur-xl transition-colors duration-200",
        interactive && "cursor-pointer hover:bg-[var(--rs-glass-strong)]",
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
