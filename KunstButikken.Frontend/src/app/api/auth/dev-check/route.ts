import { NextResponse } from "next/server";

function resolveIssuerFromEnv(): string {
  // First, try to construct from KEYCLOAK_BASE_URL + KEYCLOAK_REALM (used by AppHost)
  const baseUrl = process.env.KEYCLOAK_BASE_URL;
  const realm = process.env.KEYCLOAK_REALM || 'kunstbutikken';
  if (baseUrl) {
    const constructed = `${baseUrl}/realms/${realm}`;
    try {
      const u = new URL(constructed);
      if (u.protocol === "http:" || u.protocol === "https:") return u.toString().replace(/\/?$/, "");
    } catch {
      /* ignore invalid */
    }
  }
  
  // Fall back to trying multiple env names for direct KEYCLOAK_ISSUER
  const env = process.env as Record<string, string | undefined>;
  const candidates: Array<string | undefined> = [
    env.KEYCLOAK_ISSUER,
    env.NEXT_PUBLIC_KEYCLOAK_ISSUER,
    env.KEYCLOAK_AUTHORITY,
    env["keycloak_issuer"],
    env["NEXT_PUBLIC_keycloak_issuer"],
    env["keycloak_authority"],
  ];
  for (const c of candidates) {
    const v = (c || "").toString().trim();
    if (!v) continue;
    try {
      const u = new URL(v);
      if (u.protocol === "http:" || u.protocol === "https:") return u.toString().replace(/\/?$/, "");
    } catch {
      /* ignore invalid */
    }
  }
  return "";
}

export async function GET() {
  const isProd = process.env.NODE_ENV === "production";

  // Resolve values the same way as NextAuth route does
  const resolvedIssuer = resolveIssuerFromEnv();
  const env = process.env as Record<string, string | undefined>;
  const resolvedClientId = env.KEYCLOAK_CLIENT_ID || env.keycloak_client_id || "";
  const hasClientSecret = Boolean(env.KEYCLOAK_CLIENT_SECRET || env.keycloak_client_secret);

  const safe = {
    nodeEnv: process.env.NODE_ENV,
    nextAuthUrl: process.env.NEXTAUTH_URL || (env.nextauth_url ? "set" : "missing"),
    nextAuthSecret: process.env.NEXTAUTH_SECRET || (env.nextauth_secret ? "set" : "missing"),
    keycloak: {
      issuer: resolvedIssuer ? "set" : "missing",
      issuerValue: resolvedIssuer || null,
      clientId: resolvedClientId ? "set" : "missing",
      clientIdValue: resolvedClientId || null,
      clientSecret: hasClientSecret ? "set" : "missing",
    },
  } as const;

  if (isProd) {
    return NextResponse.json({ ok: true }, { status: 200 });
  }
  return NextResponse.json({ ok: true, env: safe }, { status: 200 });
}
