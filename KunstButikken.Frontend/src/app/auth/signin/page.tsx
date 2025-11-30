import React from "react";
import { createCurrentUserFromToken } from '@/application/security/CurrentUserFactory';
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
  let redirectTo = Array.isArray(raw) ? raw[0] : raw ?? undefined;
  const hdrs = await headers();
  const authHeader = hdrs.get('authorization') || hdrs.get('Authorization');
  const token = authHeader ? authHeader.replace(/^Bearer\s+/i, '') : null;
  const currentUser = createCurrentUserFromToken(token);
  if (!redirectTo && currentUser?.roles?.length) {
    redirectTo = '/';
  }
  const code = sp.code;
  const state = sp.state;
  return <SignInClient redirectTo={redirectTo} code={code} state={state} />;
}
