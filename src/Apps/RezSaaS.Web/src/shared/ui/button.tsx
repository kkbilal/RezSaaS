import { Slot } from "@radix-ui/react-slot";
import type { ComponentPropsWithoutRef } from "react";
import { cn } from "@/shared/lib/cn";

type ButtonVariant =
  | "primary"
  | "secondary"
  | "outline"
  | "ghost"
  | "danger"
  | "success";

type ButtonSize = "sm" | "md" | "lg";

type ButtonProps = ComponentPropsWithoutRef<"button"> & {
  asChild?: boolean;
  variant?: ButtonVariant;
  size?: ButtonSize;
};

const variants: Record<ButtonVariant, string> = {
  primary:
    "rs-gradient-bg text-white shadow-lg shadow-[rgba(99,102,241,0.28)] hover:brightness-110",
  secondary:
    "bg-[var(--rs-glass)] text-[var(--rs-ink)] border border-[var(--rs-border)] backdrop-blur-xl hover:bg-[var(--rs-glass-strong)] hover:border-[var(--rs-border-strong)]",
  outline:
    "bg-transparent text-[var(--rs-ink-soft)] border border-[var(--rs-border-strong)] hover:bg-[var(--rs-glass)] hover:text-[var(--rs-ink)]",
  ghost:
    "bg-transparent text-[var(--rs-muted)] hover:bg-[var(--rs-glass)] hover:text-[var(--rs-ink)]",
  danger:
    "bg-[var(--rs-danger-soft)] text-[var(--rs-danger)] border border-[rgba(239,68,68,0.24)] hover:bg-[rgba(239,68,68,0.24)]",
  success:
    "bg-[var(--rs-success-soft)] text-[var(--rs-success)] border border-[rgba(16,185,129,0.24)] hover:bg-[rgba(16,185,129,0.24)]"
};

const sizes: Record<ButtonSize, string> = {
  sm: "px-3 py-1.5 text-xs gap-1.5 min-h-9",
  md: "px-4 py-2 text-sm gap-2 min-h-10",
  lg: "px-5 py-2.5 text-sm gap-2 min-h-11"
};

export function Button({
  asChild = false,
  className,
  size = "md",
  variant = "primary",
  ...props
}: ButtonProps) {
  const Component = asChild ? Slot : "button";

  return (
    <Component
      className={cn(
        "inline-flex items-center justify-center rounded-xl font-medium transition-all duration-150",
        "focus:outline-none focus-visible:ring-2 focus-visible:ring-[rgba(99,102,241,0.5)]",
        "disabled:pointer-events-none disabled:opacity-40",
        variants[variant],
        sizes[size],
        className
      )}
      {...props}
    />
  );
}
