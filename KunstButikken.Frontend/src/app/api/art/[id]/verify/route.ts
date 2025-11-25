import { getBearerToken, isDevBypass } from '@/features/auth/lib/server-auth';
import { NextResponse } from 'next/server';

export const POST = async (req: Request) => {
  // Derive id from URL path
  const { pathname } = new URL(req.url);
  const parts = pathname.split('/').filter(Boolean);
  const idx = parts.findIndex((p) => p === 'art');
  const id = idx >= 0 ? parts[idx + 1] ?? '' : '';
  if (!id) {
    return new NextResponse('Bad Request - missing art ID', { status: 400 });
  }

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

  const token = getBearerToken(req);
  if (!token) {
    return new NextResponse('Unauthorized - missing Bearer token', { status: 401 });
  }

  // Forward to backend API
  const apiGateway =
    process.env.NEXT_PUBLIC_API_GATEWAY ||
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    process.env.NEXT_PUBLIC_API_ART ||
    'http://localhost:5012';
  const backendUrl = `${apiGateway.replace(/\/$/, '')}/api/art/${id}/verify`;

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

    // Some backends may return 204 No Content on success
    if (response.status === 204 || !contentType.includes('application/json')) {
      return NextResponse.json({ id, verified, updated: true }, { status: 200 });
    }

    const data = await response.json().catch(() => ({ id, verified, updated: true }));
    return NextResponse.json(data);
  } catch (error) {
    console.error('[art/verify] Error forwarding to backend:', error);
    return NextResponse.json(
      { error: 'Failed to verify art on backend' },
      { status: 500 }
    );
  }
};
