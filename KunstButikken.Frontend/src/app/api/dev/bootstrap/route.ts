export const runtime = 'nodejs';

import { NextResponse } from 'next/server';

interface UpstreamResult {
  status: number;
  ok: boolean;
  data: unknown;
}
interface ErrorResult { error: string }
interface BootstrapResults {
  keycloak?: UpstreamResult | ErrorResult;
  usersdb?: UpstreamResult | ErrorResult;
  user?: UpstreamResult | ErrorResult;
  art?: UpstreamResult | ErrorResult;
}

export async function POST(req: Request) {
  try {
    if (process.env.NODE_ENV === 'production') {
      return NextResponse.json({ error: 'Not allowed in production' }, { status: 403 });
    }

    const headers = new Headers(req.headers);
    const cookie = headers.get('cookie') || '';

    const results: BootstrapResults = {};

    // 1) Ensure demo users exist in Keycloak (dev-only)
    const userBase = process.env.NEXT_PUBLIC_API_USER?.replace(/\/$/, '');
    if (userBase) {
      try {
        const kcSeedRes = await fetch(`${userBase}/api/dev/seed-keycloak-users`, { method: 'POST' });
        const kcBody = kcSeedRes.headers.get('content-type')?.includes('application/json')
          ? await kcSeedRes.json().catch(() => ({}))
          : await kcSeedRes.text().catch(() => '');
        results.keycloak = { status: kcSeedRes.status, ok: kcSeedRes.ok, data: kcBody };
      } catch (e) {
        const message = e instanceof Error ? e.message : String(e);
        results.keycloak = { error: message };
      }

      // 2) Sync Keycloak users into usersdb (dev-only)
      try {
        const syncRes = await fetch(`${userBase}/api/dev/sync-keycloak-users`, { method: 'POST' });
        const syncBody = syncRes.headers.get('content-type')?.includes('application/json')
          ? await syncRes.json().catch(() => ({}))
          : await syncRes.text().catch(() => '');
        results.usersdb = { status: syncRes.status, ok: syncRes.ok, data: syncBody };
      } catch (e) {
        const message = e instanceof Error ? e.message : String(e);
        results.usersdb = { error: message };
      }

      // 3) Ensure current user profile exists in usersdb by calling /api/Profile/me (authorized)
      const meRes = await fetch(`${userBase}/api/Profile/me`, {
        method: 'GET',
        headers: { cookie },
        // Don't forward credentials header except cookie; CORS not relevant server-to-server
      });
      results.user = {
        status: meRes.status,
        ok: meRes.ok,
        data: meRes.ok ? await meRes.json().catch(() => ({})) : await meRes.text().catch(() => ''),
      };
    } else {
      results.user = { error: 'NEXT_PUBLIC_API_USER not configured' };
    }

    // 3) Trigger ArtService seeding from Frontend/SeedImages
    const artBase = process.env.NEXT_PUBLIC_API_ART?.replace(/\/$/, '');
    if (artBase) {
      const locale = process.env.SEED_LOCALE || process.env.NEXT_PUBLIC_SEED_LOCALE || 'en';
      const seedRes = await fetch(`${artBase}/api/seed`, {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ limit: 10, locale, force: false }),
      });
      const body = seedRes.headers.get('content-type')?.includes('application/json')
        ? await seedRes.json().catch(() => ({}))
        : await seedRes.text().catch(() => '');
      results.art = { status: seedRes.status, ok: seedRes.ok, data: body };
    } else {
      results.art = { error: 'NEXT_PUBLIC_API_ART not configured' };
    }

    return NextResponse.json({ ok: true, results });
  } catch (err) {
    console.error('[dev/bootstrap] error:', err instanceof Error ? err.stack || err.message : String(err));
    const message = err instanceof Error ? err.message : 'Unknown error';
    return NextResponse.json({ error: message }, { status: 500 });
  }
}

export async function GET(req: Request) {
  // Allow GET to trigger the same flow for convenience
  return POST(req);
}
