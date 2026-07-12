import { NextResponse, type NextRequest } from "next/server";

// ASP.NET Core Identity default cookie names
const AUTH_COOKIE_NAMES = [
  ".AspNetCore.Identity.Application",
  ".AspNetCore.Cookies"
];

const PROTECTED_PREFIXES = ["/panel", "/platform", "/hesabim", "/gelis"];
const AUTH_PREFIXES = ["/giris", "/kayit", "/sifremi-unuttum", "/sifre-sifirla"];

function hasAuthCookie(request: NextRequest): boolean {
  return AUTH_COOKIE_NAMES.some(
    (name) =>
      request.cookies.get(name)?.value !== undefined &&
      request.cookies.get(name)?.value !== ""
  );
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const isAuthed = hasAuthCookie(request);

  const isProtectedRoute = PROTECTED_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(prefix + "/")
  );

  const isAuthRoute = AUTH_PREFIXES.some(
    (prefix) => pathname === prefix || pathname.startsWith(prefix + "/")
  );

  // Unauthenticated user trying to access a protected route → redirect to login
  if (isProtectedRoute && !isAuthed) {
    const loginUrl = new URL("/giris", request.url);
    loginUrl.searchParams.set("returnTo", pathname);
    return NextResponse.redirect(loginUrl);
  }

  // Already authenticated user on an auth page (login/register) → redirect
  // to the dispatch page. The dispatch page will determine the correct
  // destination based on roles/memberships.
  if (isAuthRoute && isAuthed) {
    return NextResponse.redirect(new URL("/gelis", request.url));
  }

  return NextResponse.next();
}

export const config = {
  // Run middleware on all routes except static assets, API proxy, and
  // Next.js internals. The rewrite in next.config.ts already handles /api/*.
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - api (API routes / proxy)
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     * - sw.js, workbox-*.js (service worker)
     */
    "/((?!api|_next/static|_next/image|favicon.ico|sw.js|workbox-.*.js).*)"
  ]
};