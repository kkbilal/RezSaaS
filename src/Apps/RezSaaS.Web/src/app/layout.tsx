import type { Metadata } from "next";
import { Inter, Plus_Jakarta_Sans } from "next/font/google";
import type { ReactNode } from "react";

import { Toaster } from "@/components/ui/sonner";

import "./globals.css";

// Fontlar next/font ile SELF-HOST edilir (build aninda indirilip bundle'a girer).
// Eskiden globals.css satir 1'de harici bir Google Fonts @import'u vardi:
// render-blocking'di (LCP <= 2.5s hedefiyle celisiyor) ve her ziyaretcinin IP'sini
// Google'a sizdiriyordu (KVKK acisindan gereksiz bir ucuncu taraf aktarimi).
const inter = Inter({
  display: "swap",
  subsets: ["latin", "latin-ext"], // latin-ext: Turkce karakterler (ş, ğ, ı, ö, ü, ç)
  variable: "--rs-font-sans-src"
});

const jakarta = Plus_Jakarta_Sans({
  display: "swap",
  subsets: ["latin", "latin-ext"],
  variable: "--rs-font-display-src"
});

export const metadata: Metadata = {
  description:
    "RezSaaS; salon, spa, klinik ve stüdyo ekipleri için onaylı rezervasyon ve operasyon platformu.",
  title: {
    default: "RezSaaS",
    template: "%s | RezSaaS"
  }
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html className={`${inter.variable} ${jakarta.variable}`} lang="tr" suppressHydrationWarning>
      <body>
        {children}
        <Toaster />
      </body>
    </html>
  );
}
