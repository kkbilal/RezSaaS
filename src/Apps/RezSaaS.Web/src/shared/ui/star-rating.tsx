import { cn } from "@/shared/lib/cn";

type StarRatingProps = {
  rating: number;
  size?: "sm" | "md";
  className?: string;
};

function StarIcon({ className }: { className?: string }) {
  return (
    <svg
      aria-hidden
      viewBox="0 0 24 24"
      fill="currentColor"
      className={className}
    >
      <path d="M12 2l2.9 6.3 6.8.8-5 4.7 1.3 6.7L12 17.8 5.9 20.5 7.2 13.8l-5-4.7 6.8-.8L12 2z" />
    </svg>
  );
}

export function StarRating({ className, rating, size = "sm" }: StarRatingProps) {
  return (
    <div className={cn("flex items-center gap-1", className)}>
      <StarIcon
        className={cn(
          "text-[#fbbf24]",
          size === "sm" ? "h-3 w-3" : "h-4 w-4"
        )}
      />
      <span
        className={cn(
          "font-medium text-[var(--rs-ink)]",
          size === "sm" ? "text-xs" : "text-sm"
        )}
      >
        {rating.toFixed(1)}
      </span>
    </div>
  );
}
