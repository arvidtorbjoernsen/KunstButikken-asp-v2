import React from "react";
import SignInClient from "@/features/auth/components/SignInClient";

export default async function SignInPage({ searchParams }: { searchParams?: Promise<{ callbackUrl?: string | string[] }> }) {
  const sp = (await searchParams) ?? {};
  const raw = sp.callbackUrl;
  const redirectTo = Array.isArray(raw) ? raw[0] : raw ?? undefined;
  return <SignInClient redirectTo={redirectTo} />;
}
