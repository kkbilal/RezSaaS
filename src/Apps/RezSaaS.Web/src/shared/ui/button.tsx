import { Slot } from "@radix-ui/react-slot";
import type { ComponentPropsWithoutRef } from "react";
import { cn } from "@/shared/lib/cn";

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

type ButtonProps = ComponentPropsWithoutRef<"button"> & {
  asChild?: boolean;
  variant?: ButtonVariant;
};

const variants: Record<ButtonVariant, string> = {
  primary:
    "bg-[var(--rs-ink)] text-white shadow-[var(--rs-shadow-button)] hover:-translate-y-0.5 hover:bg-[var(--rs-ink-soft)]",
  secondary:
    "border border-[var(--rs-border)] bg-white text-[var(--rs-ink)] shadow-[var(--rs-shadow-soft)] hover:-translate-y-0.5 hover:border-[var(--rs-border-strong)]",
  ghost:
    "text-[var(--rs-muted)] hover:bg-[var(--rs-surface-muted)] hover:text-[var(--rs-ink)]",
  danger:
    "bg-[var(--rs-danger)] text-white shadow-[var(--rs-shadow-soft)] hover:-translate-y-0.5 hover:bg-[var(--rs-danger-strong)]"
};

export function Button({
  asChild = false,
  className,
  variant = "primary",
  ...props
}: ButtonProps) {
  const Component = asChild ? Slot : "button";

  return (
    <Component
      className={cn(
        "inline-flex min-h-11 items-center justify-center rounded-full px-5 text-sm font-medium transition duration-300 active:translate-y-0 active:scale-[0.98] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--rs-focus)] disabled:pointer-events-none disabled:opacity-55",
        variants[variant],
        className
      )}
      {...props}
    />
  );
}
