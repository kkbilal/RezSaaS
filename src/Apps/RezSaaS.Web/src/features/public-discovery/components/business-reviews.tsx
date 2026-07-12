"use client";

import { useState } from "react";

type BusinessReview = {
  id: string;
  rating: number;
  comment: string;
  customerDisplayName?: string | null;
  createdAt: string;
  serviceNames?: string[] | null;
};

type BusinessReviewsProps = {
  reviews?: BusinessReview[];
  ratingAverage?: number | null;
  reviewCount?: number | null;
};

export function BusinessReviews({
  reviews = [],
  ratingAverage,
  reviewCount
}: BusinessReviewsProps) {
  const [showAll, setShowAll] = useState(false);
  const hasReviews = reviews.length > 0;
  const displayReviews = showAll ? reviews : reviews.slice(0, 3);

  if (!hasReviews) {
    return null;
  }

  return (
    <section className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm font-medium uppercase tracking-[0.24em] text-[var(--rs-muted)]">
            Müşteri yorumları
          </p>
          <h2 className="mt-3 text-4xl font-semibold tracking-[-0.06em] text-[var(--rs-ink)]">
            {ratingAverage?.toFixed(1) || "0.0"} / 5
          </h2>
          <p className="mt-1 text-sm text-[var(--rs-muted)]">
            {reviewCount || 0} değerlendirme
          </p>
        </div>
        {reviews.length > 3 && !showAll && (
          <button
            className="text-sm text-[var(--rs-accent-strong)] transition hover:underline"
            onClick={() => setShowAll(true)}
          >
            Tümünü gör
          </button>
        )}
      </div>

      <div className="grid gap-4">
        {displayReviews.map((review) => (
          <ReviewCard key={review.id} review={review} />
        ))}
      </div>

      {reviews.length > 3 && showAll && (
        <button
          className="text-sm text-[var(--rs-accent-strong)] transition hover:underline"
          onClick={() => setShowAll(false)}
        >
          Daha az göster
        </button>
      )}
    </section>
  );
}

function ReviewCard({ review }: { review: BusinessReview }) {
  return (
    <div className="fade-up rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-glass)] p-6 shadow-[var(--rs-shadow-soft)]">
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <div className="flex gap-0.5">
              {[1, 2, 3, 4, 5].map((star) => (
                <svg
                  key={star}
                  className={`h-5 w-5 ${
                    star <= review.rating
                      ? "fill-yellow-400 text-yellow-400"
                      : "fill-gray-200 text-gray-200"
                  }`}
                  viewBox="0 0 20 20"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                </svg>
              ))}
            </div>
            <span className="text-sm font-medium text-[var(--rs-ink)]">
              {review.customerDisplayName || "Misafir"}
            </span>
          </div>
          
          <p className="mt-3 text-sm leading-6 text-[var(--rs-muted-strong)]">
            {review.comment}
          </p>

          {review.serviceNames && review.serviceNames.length > 0 && (
            <div className="mt-3 flex flex-wrap gap-2">
              {review.serviceNames.map((serviceName) => (
                <span
                  key={serviceName}
                  className="rounded-full bg-[var(--rs-neutral-soft)] px-3 py-1 text-xs text-[var(--rs-muted)]"
                >
                  {serviceName}
                </span>
              ))}
            </div>
          )}

          <p className="mt-4 text-xs text-[var(--rs-muted)]">
            {formatDate(review.createdAt)}
          </p>
        </div>
      </div>
    </div>
  );
}

function formatDate(dateString: string) {
  try {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays === 0) {
      return "Bugün";
    }

    if (diffDays === 1) {
      return "Dün";
    }

    if (diffDays < 7) {
      return `${diffDays} gün önce`;
    }

    if (diffDays < 30) {
      const weeks = Math.floor(diffDays / 7);
      return `${weeks} hafta önce`;
    }

    if (diffDays < 365) {
      const months = Math.floor(diffDays / 30);
      return `${months} ay önce`;
    }

    const years = Math.floor(diffDays / 365);
    return `${years} yıl önce`;
  } catch {
    return dateString;
  }
}