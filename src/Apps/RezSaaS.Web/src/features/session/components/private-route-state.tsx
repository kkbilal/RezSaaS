import Link from "next/link";
import { routes } from "@/shared/config/routes";
import { Button } from "@/shared/ui/button";
import { Card, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card";

type PrivateRouteStateProps = {
  actionHref?: string;
  actionLabel?: string;
  description: string;
  eyebrow?: string;
  title: string;
};

export function PrivateRouteState({
  actionHref = routes.auth.login,
  actionLabel = "Girişe git",
  description,
  eyebrow = "Güvenli kapı",
  title
}: PrivateRouteStateProps) {
  return (
    <main className="studio-grid grid min-h-screen place-items-center px-4 py-10">
      <Card className="fade-up w-full max-w-2xl p-7 sm:p-9">
        <CardHeader>
          <p className="w-fit rounded-full bg-[var(--rs-accent-soft)] px-4 py-2 text-sm font-medium text-[var(--rs-accent-strong)]">
            {eyebrow}
          </p>
          <CardTitle className="mt-5 text-4xl sm:text-5xl">{title}</CardTitle>
          <CardDescription className="max-w-xl">{description}</CardDescription>
        </CardHeader>
        <div className="mt-7">
          <Button asChild>
            <Link href={actionHref}>{actionLabel}</Link>
          </Button>
        </div>
      </Card>
    </main>
  );
}
