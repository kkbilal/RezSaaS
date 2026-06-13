import { redirect } from "next/navigation";
import type { Metadata } from "next";
import { routes } from "@/shared/config/routes";

export const metadata: Metadata = {
  robots: {
    index: false
  },
  title: "Platform"
};

export default function PlatformRoute() {
  redirect(routes.platform.abuse);
}
