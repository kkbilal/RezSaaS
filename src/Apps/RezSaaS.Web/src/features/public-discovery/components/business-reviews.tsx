"use client";

import { useState } from "react";
import { Star } from "lucide-react";
import type { PublicReviewSummary } from "@/features/public-discovery/api/public-businesses";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";

type PublicReview = NonNullable<PublicReviewSummary["reviews"]>[number];

type BusinessReviewsProps = {
  summary: PublicReviewSummary;
};

const initialVisibleCount = 3;

export function BusinessReviews({ summary }: BusinessReviewsProps) {
  const [showAll, setShowAll] = useState(false);
  const reviews = summary.reviews ?? [];

  // Yorum METNI yoksa bolum hic cizilmez. "Henuz yorum yok" bos kutusu, urunun en kritik
  // edinim yuzeyinde olumsuz bir ilk izlenim uretir -- yeni salonun sucu degil, gostermeyiz.
  if (reviews.length === 0) {
    return null;
  }

  const visibleReviews = showAll ? reviews : reviews.slice(0, initialVisibleCount);
  const averageRating = summary.averageRating ?? 0;
  const totalCount = summary.totalCount ?? reviews.length;

  return (
    <section aria-labelledby="yorumlar-basligi" className="space-y-4" id="yorumlar">
      <div>
        <h2
          className="text-xl font-semibold tracking-tight text-foreground"
          id="yorumlar-basligi"
        >
          Müşteri yorumları
        </h2>
        <div className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-1">
          <RatingStars rating={averageRating} />
          {/* Puan METIN olarak da yazilir: yildiz TEK sinyal olamaz (ekran okuyucu, renk korlugu). */}
          <span className="text-sm font-medium text-foreground">
            {averageRating.toFixed(1)} / 5
          </span>
          <span className="text-sm text-muted-foreground">
            · {totalCount} değerlendirme
          </span>
        </div>
      </div>

      <div className="grid gap-3">
        {visibleReviews.map((review) => (
          <ReviewCard key={review.id} review={review} />
        ))}
      </div>

      {reviews.length > initialVisibleCount ? (
        <Button
          className="min-h-11 w-full sm:w-auto"
          onClick={() => setShowAll((current) => !current)}
          type="button"
          variant="outline"
        >
          {showAll
            ? "Daha az göster"
            : `Tüm yorumları gör (${reviews.length})`}
        </Button>
      ) : null}

      {/* Sayfalama YOK: uc page/pageSize aliyor ama ilk 10'u cekiyoruz. Daha fazlasi icin
          ayri bir istek gerekir; bunu VAAT ETMEYELIM diye sayiyi acikca yaziyoruz. */}
      {totalCount > reviews.length ? (
        <p className="text-sm text-muted-foreground">
          En son {reviews.length} yorum gösteriliyor.
        </p>
      ) : null}
    </section>
  );
}

function ReviewCard({ review }: { review: PublicReview }) {
  const rating = review.rating ?? 0;

  return (
    <Card>
      <CardContent className="space-y-3">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
          <RatingStars rating={rating} />
          <span className="text-sm font-medium text-foreground">
            {review.customerDisplayName || "Misafir"}
          </span>
          <span className="text-sm text-muted-foreground">
            {formatReviewDate(review.createdAtUtc)}
          </span>
        </div>

        {review.comment ? (
          <>
            <Separator />
            <p className="text-sm leading-6 text-muted-foreground">
              {review.comment}
            </p>
          </>
        ) : null}
      </CardContent>
    </Card>
  );
}

function RatingStars({ rating }: { rating: number }) {
  const rounded = Math.round(rating);

  return (
    // Gorsel sus: gercek deger her zaman yanindaki metinde yazili.
    <span aria-hidden="true" className="flex gap-0.5">
      {[1, 2, 3, 4, 5].map((star) => (
        <Star
          className={
            star <= rounded
              ? "size-4 fill-amber-500 text-amber-500"
              : "size-4 fill-muted text-muted-foreground/40"
          }
          key={star}
        />
      ))}
    </span>
  );
}

// Yorum tarihi MUTLAK gosterilir. "3 gun once" gibi goreli metin sunucuda ve tarayicida
// farkli hesaplanip hydration uyusmazligi uretir; yorum icin kesin tarih zaten yeterli.
function formatReviewDate(value?: string) {
  if (!value) {
    return "";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    day: "numeric",
    month: "long",
    year: "numeric"
  }).format(date);
}
