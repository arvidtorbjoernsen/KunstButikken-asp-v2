import { getBearerToken, isDevBypass } from '@/features/auth/lib/server-auth';
import { NextResponse } from 'next/server';

export const POST = async (req: Request) => {
  // Derive id from URL path
  const { pathname } = new URL(req.url);
  const parts = pathname.split('/').filter(Boolean);
  const idx = parts.findIndex((p) => p === 'profile');
  const id = idx >= 0 ? parts[idx + 1] ?? '' : '';
  if (!id) {
    return new NextResponse('Bad Request - missing profile ID', { status: 400 });
  }

  // Parse body early so the dev-bypass can inspect `verified`
  let verified = false;
  try {
    const body = (await req.json()) as { verified?: boolean };
    verified = Boolean(body?.verified);
  } catch {
    return new NextResponse('Bad Request - invalid JSON body', { status: 400 });
  }

  // Development-only bypass
  if (isDevBypass(req)) {
    return NextResponse.json({ id, verified, message: 'Dev bypass: verification updated' });
  }

  // Require a Bearer token header
  const token = getBearerToken(req);
  if (!token) {
    return new NextResponse('Unauthorized - missing Bearer token', { status: 401 });
  }

  // Forward to backend API
  const apiGateway =
    process.env.NEXT_PUBLIC_API_GATEWAY ||
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    process.env.NEXT_PUBLIC_API_USER ||
    'http://localhost:5011';
  const backendUrl = `${apiGateway.replace(/\/$/, '')}/api/profile/${id}/verify`;

  try {
    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ verified }),
    });

    const contentType = response.headers.get('content-type') || '';

    if (!response.ok) {
      const text = await response.text().catch(() => '');
      return new NextResponse(text || response.statusText || 'Backend verification failed', { status: response.status });
    }

    // Success with no content or non-JSON -> return a simple success payload
    if (response.status === 204 || !contentType.includes('application/json')) {
      return NextResponse.json({ id, verified, updated: true }, { status: 200 });
    }

    const data = await response.json().catch(() => ({ id, verified, updated: true }));
    return NextResponse.json(data);
  } catch (error) {
    console.error('[profile/verify] Error forwarding to backend:', error);
    return NextResponse.json(
      { error: 'Failed to verify profile on backend' },
      { status: 500 }
    );
  }
};
