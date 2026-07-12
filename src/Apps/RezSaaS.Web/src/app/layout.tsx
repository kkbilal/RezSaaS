import type { Metadata } from "next";
import type { ReactNode } from "react";
import { AnimatedBackground } from "@/shared/ui/animated-background";
import "./globals.css";

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
    <html lang="tr">
      <body>
        <AnimatedBackground />
        {children}
      </body>
    </html>
  );
}
