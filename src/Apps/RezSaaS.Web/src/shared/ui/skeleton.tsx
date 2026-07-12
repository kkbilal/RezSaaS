import { type HTMLAttributes } from "react";
import { cn } from "@/shared/lib/cn";

const baseClass =
  "animate-pulse bg-[var(--rs-surface-muted)] rounded-[var(--rs-radius)]";

export function CardSkeleton({
  className,
  ...props
}: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("rounded-2xl border border-[var(--rs-border)] p-5", baseClass, className)}
      {...props}
    />
  );
}

export function TextSkeleton({
  className,
  ...props
}: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("rounded-[var(--rs-radius)]", baseClass, className)}
      {...props}
    />
  );
}

export function ButtonSkeleton({
  className,
  ...props
}: HTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      className={cn("min-h-12 w-full rounded-2xl", baseClass, className)}
      disabled
      type="button"
      {...props}
    />
  );
}