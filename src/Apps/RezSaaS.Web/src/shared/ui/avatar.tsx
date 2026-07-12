import { cn } from "@/shared/lib/cn";

type AvatarSize = "xs" | "sm" | "md" | "lg";

type AvatarProps = {
  name: string;
  src?: string | null;
  size?: AvatarSize;
  className?: string;
};

const sizeClasses: Record<AvatarSize, string> = {
  xs: "h-6 w-6 text-[10px]",
  sm: "h-7 w-7 text-xs",
  md: "h-9 w-9 text-sm",
  lg: "h-12 w-12 text-base"
};

function getInitials(name: string): string {
  const trimmed = name.trim();

  if (!trimmed) {
    return "?";
  }

  const parts = trimmed.split(/\s+/).filter(Boolean);
  const firstPart = parts[0];

  if (parts.length === 1 && firstPart) {
    return firstPart.slice(0, 2).toUpperCase();
  }

  const first = firstPart?.[0] ?? "";
  const last = parts[parts.length - 1]?.[0] ?? "";

  return (first + last).toUpperCase();
}

export function Avatar({ className, name, size = "md", src }: AvatarProps) {
  const initials = getInitials(name);

  if (src) {
    return (
      // eslint-disable-next-line @next/next/no-img-element
      <img
        alt={name}
        className={cn(
          "shrink-0 rounded-full bg-[var(--rs-accent-soft)] object-cover",
          sizeClasses[size],
          className
        )}
        src={src}
      />
    );
  }

  return (
    <div
      aria-label={name}
      role="img"
      className={cn(
        "rs-gradient-bg flex shrink-0 items-center justify-center rounded-full font-semibold text-white",
        sizeClasses[size],
        className
      )}
    >
      <span aria-hidden>{initials}</span>
    </div>
  );
}
