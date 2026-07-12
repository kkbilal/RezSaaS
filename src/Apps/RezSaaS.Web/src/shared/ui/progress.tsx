import { cn } from "@/shared/lib/cn";

type ProgressStep = {
  label: string;
  state: "complete" | "current" | "upcoming";
};

type ProgressProps = {
  steps: ReadonlyArray<ProgressStep>;
  className?: string;
};

export function Progress({ className, steps }: ProgressProps) {
  const total = steps.length;

  return (
    <ol
      className={cn("flex flex-col gap-3 sm:flex-row sm:items-center", className)}
    >
      {steps.map((step, index) => (
        <li
          key={`${step.label}-${index}`}
          className={cn(
            "flex items-center gap-3",
            index < total - 1 ? "sm:flex-1" : ""
          )}
        >
          <span className="flex items-center gap-2">
            <span
              aria-current={step.state === "current" ? "step" : undefined}
              className={cn(
                "flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-semibold transition",
                step.state === "complete" &&
                  "bg-[var(--rs-accent)] text-white",
                step.state === "current" &&
                  "border-2 border-[var(--rs-accent)] bg-[var(--rs-accent-soft)] text-[var(--rs-accent-strong)]",
                step.state === "upcoming" &&
                  "border border-[var(--rs-border)] bg-[var(--rs-glass)] text-[var(--rs-muted)] backdrop-blur-xl"
              )}
            >
              {step.state === "complete" ? "✓" : index + 1}
            </span>
            <span
              className={cn(
                "text-xs font-medium tracking-[0.02em] sm:text-sm",
                step.state === "upcoming" ? "text-[var(--rs-muted)]" : "text-[var(--rs-ink)]"
              )}
            >
              {step.label}
            </span>
          </span>
          {index < total - 1 ? (
          <span
            aria-hidden
            className={cn(
              "hidden h-px flex-1 sm:block",
              step.state === "complete"
                ? "bg-[var(--rs-accent)]"
                : "bg-[var(--rs-border)]"
            )}
          />
          ) : null}
        </li>
      ))}
    </ol>
  );
}
