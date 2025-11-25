import { NextResponse } from "next/server";

// Development-only seeding endpoint placeholder
// NOTE: Seeding is handled by backend APIs and Keycloak, not the frontend
// This endpoint exists for future frontend-specific dev utilities if needed

export async function POST() {
  const isDev = process.env.NODE_ENV !== "production";
  const allow = process.env.ALLOW_DEV_SEED === "true" || isDev;
  if (!allow) {
    return NextResponse.json(
      { error: "Seeding is disabled. Set ALLOW_DEV_SEED=true to enable in non-dev environments." },
      { status: 403 }
    );
  }

  return NextResponse.json({
    ok: true,
    message: "Seeding is handled by backend services and Keycloak, not the frontend.",
    notes: [
      "Backend APIs handle user and art seeding automatically",
      "Admin dashboard shows real registered sellers and artwork",
      "Use backend seeding mechanisms to populate test data"
    ]
  });
}
