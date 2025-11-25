// Note: don't import NextResponse here because we return a standard Response
// and NextResponse is unused. Keeping the file minimal prevents unused-import warnings.

// Accept log posts from next-auth client and return a JSON body to avoid
// client-side JSON parse errors when it calls response.json().
export async function POST(req: Request) {
  try {
    // Read the request text once and parse if possible.
    let payload: unknown = null;
    try {
      const text = await req.text();
      try {
        payload = text ? (JSON.parse(text) as unknown) : null;
      } catch {
        // Not JSON, return raw text
        payload = text || null;
      }
    } catch {
      payload = null;
    }

    const body = JSON.stringify({ ok: true, received: payload } satisfies { ok: boolean; received: unknown });
    return new Response(body, { status: 200, headers: { 'content-type': 'application/json' } });
  } catch {
    const errBody = JSON.stringify({ error: 'Bad Request' });
    return new Response(errBody, { status: 400, headers: { 'content-type': 'application/json' } });
  }
}
