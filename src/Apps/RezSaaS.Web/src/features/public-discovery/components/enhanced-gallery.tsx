"use client";

import { useState } from "react";
import { DialogOverlay, DialogPanel } from "@/shared/ui/dialog";

type GalleryImage = {
  imageUrl?: string | null;
  altText?: string | null;
  sortOrder?: number;
};

type EnhancedGalleryProps = {
  images: GalleryImage[];
};

export function EnhancedGallery({ images }: EnhancedGalleryProps) {
  const [selectedImageIndex, setSelectedImageIndex] = useState<number | null>(null);

  if (images.length === 0) {
    return null;
  }

  const displayImages = images.slice(0, 6); // Show max 6 images
  const hasMoreImages = images.length > 6;

  return (
    <>
      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <p className="text-sm font-medium uppercase tracking-[0.24em] text-[var(--rs-muted)]">
            Galeri
          </p>
          {hasMoreImages && (
            <p className="text-sm text-[var(--rs-muted)]">
              +{images.length - 6} daha fazla
            </p>
          )}
        </div>
        
        <div className="grid gap-4 lg:grid-cols-3">
          {displayImages.map((image, index) => {
            const imageUrl = getSafeImageUrl(image.imageUrl);
            const isFirst = index === 0;
            const isLarge = index === 0 && displayImages.length >= 2;
            
            return (
              <div
                key={`${image.imageUrl}-${index}`}
                onClick={() => setSelectedImageIndex(index)}
                className={`fade-up group relative overflow-hidden rounded-[2rem] border border-[var(--rs-border)] bg-[var(--rs-surface)] shadow-[var(--rs-shadow-soft)] cursor-pointer transition-all duration-300 hover:-translate-y-1 hover:shadow-[var(--rs-shadow-card)] ${
                  isLarge ? 'lg:row-span-2 lg:col-span-2' : ''
                }`}
                style={{ animationDelay: `${index * 70}ms` }}
              >
                {imageUrl ? (
                  <>
                    <img
                      alt={image.altText ?? "İşletme galerisi"}
                      className={`h-full w-full object-cover transition-transform duration-500 group-hover:scale-110 ${
                        isLarge ? 'min-h-[512px]' : 'h-64'
                      }`}
                      decoding="async"
                      fetchPriority={isFirst ? "high" : "auto"}
                      height={isLarge ? 512 : 256}
                      loading={isFirst ? "eager" : "lazy"}
                      referrerPolicy="no-referrer"
                      sizes="(min-width: 1024px) 33vw, 100vw"
                      src={imageUrl}
                      width={640}
                    />
                    {/* Hover overlay */}
                    <div className="absolute inset-0 bg-black/0 transition-all duration-300 group-hover:bg-black/20">
                      <div className="absolute inset-0 flex items-center justify-center opacity-0 transition-opacity duration-300 group-hover:opacity-100">
                        <svg
                          className="h-12 w-12 text-white"
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM10 7v3m0 0v3m0-3h3m-3 0H7"
                          />
                        </svg>
                      </div>
                    </div>
                  </>
                ) : (
                  <div className={`grid place-items-center bg-[var(--rs-surface-muted)] px-6 text-center text-sm text-[var(--rs-muted)] ${
                    isLarge ? 'min-h-[512px]' : 'h-64'
                  }`}>
                    {image.altText ?? "Galeri görseli"}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </section>

      {/* Lightbox */}
      {selectedImageIndex !== null && (
        <div 
          className="fixed inset-0 z-50 grid place-items-center bg-[rgb(5_26_36_/_0.42)] p-4 backdrop-blur-sm"
          onClick={() => setSelectedImageIndex(null)}
        >
          <Lightbox
            images={displayImages}
            selectedIndex={selectedImageIndex}
            onClose={() => setSelectedImageIndex(null)}
            onNext={() => setSelectedImageIndex((prev) => 
              prev !== null ? (prev + 1) % displayImages.length : null
            )}
            onPrevious={() => setSelectedImageIndex((prev) => 
              prev !== null ? (prev - 1 + displayImages.length) % displayImages.length : null
            )}
          />
        </div>
      )}
    </>
  );
}

function Lightbox({
  images,
  selectedIndex,
  onClose,
  onNext,
  onPrevious
}: {
  images: GalleryImage[];
  selectedIndex: number;
  onClose: () => void;
  onNext: () => void;
  onPrevious: () => void;
}) {
  const selectedImage = images[selectedIndex];
  const imageUrl = getSafeImageUrl(selectedImage?.imageUrl);

  return (
    <DialogOverlay onEscapeKeyDown={onClose}>
      <DialogPanel className="max-w-6xl p-0" onClick={(e) => e.stopPropagation()}>
        {/* Image */}
        {imageUrl ? (
          <img
            alt={selectedImage?.altText ?? "İşletme galerisi"}
            className="max-h-[80vh] w-full object-contain"
            src={imageUrl}
          />
        ) : (
          <div className="grid min-h-[400px] place-items-center bg-[var(--rs-surface-muted)] text-sm text-[var(--rs-muted)]">
            {selectedImage?.altText ?? "Galeri görseli"}
          </div>
        )}

        {/* Navigation buttons */}
        {images.length > 1 && (
          <>
            <button
              className="absolute left-4 top-1/2 -translate-y-1/2 rounded-full bg-black/50 p-3 text-white transition hover:bg-black/70 focus:outline-none focus:ring-2 focus:ring-white"
              onClick={onPrevious}
              aria-label="Önceki görsel"
            >
              <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
            </button>
            <button
              className="absolute right-4 top-1/2 -translate-y-1/2 rounded-full bg-black/50 p-3 text-white transition hover:bg-black/70 focus:outline-none focus:ring-2 focus:ring-white"
              onClick={onNext}
              aria-label="Sonraki görsel"
            >
              <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </button>
          </>
        )}

        {/* Close button */}
        <button
          className="absolute right-4 top-4 rounded-full bg-black/50 p-3 text-white transition hover:bg-black/70 focus:outline-none focus:ring-2 focus:ring-white"
          onClick={onClose}
          aria-label="Kapat"
        >
          <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>

        {/* Image counter */}
        {images.length > 1 && (
          <div className="absolute bottom-4 left-1/2 -translate-x-1/2 rounded-full bg-black/50 px-4 py-2 text-sm text-white">
            {selectedIndex + 1} / {images.length}
          </div>
        )}
      </DialogPanel>
    </DialogOverlay>
  );
}

function getSafeImageUrl(value?: string | null) {
  if (!value) {
    return null;
  }

  if (value.startsWith("/") || value.startsWith("https://")) {
    return value;
  }

  return null;
}