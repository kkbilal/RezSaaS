import Link from "next/link";

import { Button } from "@/components/ui/button";
import { routes } from "@/shared/config/routes";

// Public yuzeyin ortak basligi: / ve /kesfet bunu paylasir.
//
// Neden SERVER component (eski shared/ui/public-navbar.tsx client'ti):
// - Public sayfalar urunun edinim kanali; SSR/indexlenebilir kalmali. Scroll dinleyen
//   bir "use client" navbar, sayfanin en ustundeki bloku gereksizce client'a tasiyordu.
// - Tek state'i scroll-blur ve mobil menuydu. Blur/glass zaten light-first kararinda
//   (docs/29) yasak; menu ise 2 linke indi -> state'e gerek kalmadi.
//
// Neden "Isletmeler icin" linki YOK: arkasinda self-servis isletme kaydi yok (docs/29 K3).
// Bkz. app/page.tsx icindeki uzun gerekce.
export function PublicHeader() {
  return (
    <header className="border-b border-border bg-background">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-4 sm:px-6">
        <Link
          className="flex min-h-11 items-center rounded-md text-lg font-semibold tracking-tight text-foreground focus-visible:ring-[3px] focus-visible:ring-ring/50 focus-visible:outline-none"
          href={routes.public.home}
        >
          RezSaaS
        </Link>

        <nav className="flex items-center gap-1 sm:gap-2">
          <Button asChild className="min-h-11 px-3 sm:px-4" variant="ghost">
            <Link href={routes.public.discover}>Keşfet</Link>
          </Button>
          {/* Giris DOGRU hedef: elle kurulan isletme sahibi de, musteri de buradan girer. */}
          <Button asChild className="min-h-11 px-3 sm:px-4" variant="default">
            <Link href={routes.auth.login}>Giriş yap</Link>
          </Button>
        </nav>
      </div>
    </header>
  );
}
