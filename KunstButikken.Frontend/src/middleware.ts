import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

const PROTECTED_ROUTE_PATTERNS = [
  /^\/auth\/me(\/.*)?$/,
  /^\/profile(\/.*)?$/,
  /^\/admin(\/.*)?$/,
  /^\/seller(\/.*)?$/,
];

function requiresAuth(pathname: string): boolean {
  return PROTECTED_ROUTE_PATTERNS.some(pattern => pattern.test(pathname));
}

function requiresGatewaySession(pathname: string): boolean {
  return pathname.startsWith('/admin') || pathname.startsWith('/seller');
}

function ensureRedirectPath(value: string | null, request: NextRequest): string | null {
  if (!value) {
    return null;
  }
  try {
    const url = value.startsWith('http') ? new URL(value) : new URL(value, request.nextUrl.origin);
    if (url.origin !== request.nextUrl.origin) {
      return null;
    }
    return url.pathname + url.search + url.hash;
  } catch {
    if (value.startsWith('/')) {
      return value;
    }
    return null;
  }
}

async function validateSessionForRoute(
  pathname: string,
  cookies: NextRequest['cookies'],
): Promise<boolean> {
  if (!requiresGatewaySession(pathname)) {
    return true;
  }
  const session = cookies.get('AUTHGATEWAY_SESSION')?.value;
  if (!session) {
    return false;
  }
  try {
    const resp = await fetch(`${process.env.NEXT_PUBLIC_API_GATEWAY ?? ''}/auth/session/validate`, {
      method: 'GET',
      credentials: 'include',
      cache: 'no-store',
    });
    return resp.ok;
  } catch (err) {
    console.error('[middleware] Session validation failed:', err);
    return false;
  }
}

export async function middleware(request: NextRequest) {
  const { nextUrl, cookies } = request;
  const pathname = nextUrl.pathname;

  if (!requiresAuth(pathname)) {
    return NextResponse.next();
  }

  const hasGatewaySession = Boolean(cookies.get('AUTHGATEWAY_SESSION')?.value);
  const hasFallbackSession = Boolean(cookies.get('AUTHGATEWAY_REFRESH')?.value || cookies.get('KEYCLOAK_SESSION')?.value);
  const needsGateway = requiresGatewaySession(pathname);

  if (needsGateway && hasGatewaySession) {
    const valid = await validateSessionForRoute(pathname, cookies);
    if (!valid) {
      const forbiddenUrl = new URL('/auth/forbidden', request.url);
      forbiddenUrl.searchParams.set('redirect', pathname);
      return NextResponse.redirect(forbiddenUrl);
    }
  }

  if ((needsGateway && !hasGatewaySession) || (!needsGateway && !hasGatewaySession && !hasFallbackSession)) {
    const redirectUrl = new URL('/auth/signin', request.url);
    const safeRedirect = ensureRedirectPath(request.nextUrl.searchParams.get('redirect'), request) ?? pathname;
    redirectUrl.searchParams.set('redirect', safeRedirect);
    return NextResponse.redirect(redirectUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/auth/me', '/auth/me/:path*', '/profile', '/profile/:path*', '/admin/:path*', '/seller/:path*'],
};

// TODO: Expand matcher config for other auth-required routes once AuthGateway exposes more SSR endpoints
