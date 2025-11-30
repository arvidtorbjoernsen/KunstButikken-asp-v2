import React from "react";
import { headers } from "next/headers";
import SignInClient from "@/features/auth/components/SignInClient";

interface SignInSearchParams {
  callbackUrl?: string | string[];
  code?: string;
  state?: string;
}

export default async function SignInPage({ searchParams }: { searchParams?: Promise<SignInSearchParams> }) {
  const sp = (await searchParams) ?? {};
  const raw = sp.callbackUrl;
  const redirectTo = Array.isArray(raw) ? raw[0] : raw ?? undefined;
  const code = sp.code;
  const state = sp.state;
  return <SignInClient redirectTo={redirectTo} code={code} state={state} />;
}
