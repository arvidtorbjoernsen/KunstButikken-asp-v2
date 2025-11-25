import { NextResponse } from 'next/server';

// Return a minimal JSON error response so the NextAuth client can parse it.
export async function GET(req: Request) {
  try {
    const url = new URL(req.url);
    const error = url.searchParams.get('error') || url.searchParams.get('message') || 'Unknown error';
    return NextResponse.json({ error, message: error }, { status: 200 });
  } catch {
    return NextResponse.json({ error: 'Bad Request' }, { status: 400 });
  }
}

// Also accept POST from the client (some NextAuth client flows POST to the error endpoint).
export async function POST(req: Request) {
  try {
    // Try to parse JSON body, fall back to text and query params if needed
    let payload: unknown = null;
    try {
      payload = await req.json();
    } catch {
      try {
        const txt = await req.text();
        payload = txt ? { message: txt } : null;
      } catch {
        payload = null;
      }
    }

    const parsed = (payload && typeof payload === 'object') ? (payload as Record<string, unknown>) : undefined;
    const url = new URL(req.url);
    const error = (parsed?.error as string) || (parsed?.message as string) || url.searchParams.get('error') || 'Unknown error';
    return NextResponse.json({ error, message: error }, { status: 200 });
  } catch {
    return NextResponse.json({ error: 'Bad Request' }, { status: 400 });
  }
}
