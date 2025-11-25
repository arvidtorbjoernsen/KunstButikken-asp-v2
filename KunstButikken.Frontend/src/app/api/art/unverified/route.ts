import { getBearerToken, isDevBypass } from '@/features/auth/lib/server-auth';
import { NextResponse } from 'next/server';

// Fetch unverified art items from the ArtService
export async function GET(req: Request) {
  try {
    const url = new URL(req.url);
    const dbg = url.searchParams.get('dbg');
    const hdr = req.headers.get('x-dev-admin');
    if (dbg === '1') {
      return NextResponse.json({ dbg: true, NODE_ENV: process.env.NODE_ENV, xDevAdminHeader: hdr ?? null, NEXT_PUBLIC_API_ART: process.env.NEXT_PUBLIC_API_ART ?? null });
    }
  } catch {
    // ignore
  }

  // Development-only bypass
  if (isDevBypass(req)) {
    const devStub = [
      { id: '1', titleEn: 'Dev Art A', titleNb: 'Dev Kunst A', artist: 'Dev Artist', verified: false },
      { id: '2', titleEn: 'Dev Art B', titleNb: 'Dev Kunst B', artist: 'Dev Artist', verified: false },
    ];
    return NextResponse.json(devStub);
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
  const backendUrl = `${apiGateway.replace(/\/$/, '')}/api/art/unverified`;

  try {
    const response = await fetch(backendUrl, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    const contentType = response.headers.get('content-type') || '';

    // If response is not OK, forward status and text without throwing JSON parse
    if (!response.ok) {
      const text = await response.text().catch(() => '');
      if (text) {
        console.error('[art/unverified] Backend error', { status: response.status, text: text.slice(0, 300) });
        return new NextResponse(text, { status: response.status });
      }
      return new NextResponse(response.statusText || 'Upstream error', { status: response.status });
    }

    // OK: try to return JSON when content type allows; otherwise return empty array fallback
    if (contentType.includes('application/json')) {
      const data = await response.json();
      return NextResponse.json(data, { status: response.status });
    }

    // Non-JSON but OK response: return empty list to UI
    return NextResponse.json([], { status: 200 });
  } catch (error) {
    console.error('[art/unverified] Error forwarding to backend:', error);
    return NextResponse.json(
      { error: 'Failed to fetch unverified art from backend' },
      { status: 500 }
    );
  }
}
