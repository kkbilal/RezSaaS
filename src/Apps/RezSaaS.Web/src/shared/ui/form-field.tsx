import type { InputHTMLAttributes, ReactNode } from "react";
import { cn } from "@/shared/lib/cn";

type FormFieldProps = {
  children: ReactNode;
  error?: string | null;
  hint?: string;
  label: string;
};

export function FormField({ children, error, hint, label }: FormFieldProps) {
  return (
    <label className="block space-y-2">
      <span className="text-sm font-medium text-[var(--rs-ink)]">{label}</span>
      {children}
      {hint ? <span className="block text-xs text-[var(--rs-muted)]">{hint}</span> : null}
      {error ? <span className="block text-xs text-[var(--rs-danger)]">{error}</span> : null}
    </label>
  );
}

export function TextInput({
  className,
  ...props
}: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      className={cn(
        "min-h-12 w-full rounded-2xl border border-[var(--rs-border)] bg-[var(--rs-glass)] px-4 text-sm text-[var(--rs-ink)] backdrop-blur-xl outline-none transition placeholder:text-[var(--rs-muted)] focus:border-[var(--rs-accent)] focus:ring-4 focus:ring-[rgba(99_102_241_/_0.18)] disabled:cursor-not-allowed disabled:opacity-60",
        className
      )}
      {...props}
    />
  );
}
