import { getBearerToken, isDevBypass } from '@/features/auth/lib/server-auth';
import { NextResponse } from 'next/server';

// Fetch unverified seller profiles from the UserService (DB-backed)
export async function GET(req: Request) {
  // Temporary debug: if dbg=1 or x-dev-admin=1, return environment and header info.
  try {
    const url = new URL(req.url);
    const dbg = url.searchParams.get('dbg');
    const hdr = req.headers.get('x-dev-admin');
    // Only return debug info when explicitly requested with ?dbg=1.
    if (dbg === '1') {
      return NextResponse.json({ dbg: true, NODE_ENV: process.env.NODE_ENV, xDevAdminHeader: hdr ?? null, NEXT_PUBLIC_API_USER: process.env.NEXT_PUBLIC_API_USER ?? null });
    }
  } catch {
    // ignore
  }

  // Development-only admin bypass for local testing: send header `x-dev-admin: 1`.
  // This is explicitly only enabled when NODE_ENV !== 'production'.
  if (isDevBypass(req)) {
    // Always return the dev stub list when the x-dev-admin header is present
    // to make local frontend testing simple and reliable (no upstream calls).
    const devStub = [
      { id: '1', userId: '1', displayName: 'Dev Seller A', email: 'sellerA@test.com', verified: false },
      { id: '2', userId: '2', displayName: 'Dev Seller B', email: 'sellerB@test.com', verified: false },
    ];
    return NextResponse.json(devStub);
  }
  
  const token = getBearerToken(req);
  if (!token) {
    return new NextResponse('Unauthorized - missing Bearer token', { status: 401 });
  }

  // Forward to backend API
  const apiGateway = process.env.NEXT_PUBLIC_API_GATEWAY || process.env.NEXT_PUBLIC_API_USER || 'http://localhost:5011';
  const backendUrl = `${apiGateway}/api/profile/unverified`;

  try {
    const response = await fetch(backendUrl, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    const data = await response.json();
    
    return NextResponse.json(data, { status: response.status });
  } catch (error) {
    console.error('[profile/unverified] Error forwarding to backend:', error);
    return NextResponse.json(
      { error: 'Failed to fetch unverified sellers from backend' },
      { status: 500 }
    );
  }
}
