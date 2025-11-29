"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { useKeycloak } from "@/features/auth/lib/keycloak";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { exchangeCodeForGatewaySession } from "@/features/auth/lib/gateway-session";

type SignInClientProps = {
  redirectTo?: string;
  code?: string;
  state?: string;
};

export default function SignInClient({ redirectTo, code }: SignInClientProps) {
  const { t } = useTranslations();
  const { keycloak } = useKeycloak();
  const router = useRouter();
  const hasTriggeredRef = useRef(false);
  const isRedirectingRef = useRef(false);
  const [isExchanging, setIsExchanging] = useState(false);
  const [exchangeFailed, setExchangeFailed] = useState(false);

  useEffect(() => {
    const redirectTarget = redirectTo || '/';

    if (code && !hasTriggeredRef.current) {
      hasTriggeredRef.current = true;
      const controller = new AbortController();
      setIsExchanging(true);
      exchangeCodeForGatewaySession(code, `${window.location.origin}${redirectTarget}`, controller.signal)
        .then(() => {
          console.log('[SignInClient] Gateway session established, redirecting to:', redirectTarget);
          router.replace(redirectTarget);
        })
        .catch(err => {
          console.error('[SignInClient] Failed to exchange auth code:', err);
          setExchangeFailed(true);
          router.replace('/auth/error?reason=session');
        })
        .finally(() => setIsExchanging(false));
      return () => controller.abort();
    }

    // If already authenticated, redirect to the intended destination (ONCE)
    if (keycloak?.authenticated && !isRedirectingRef.current) {
      console.log('[SignInClient] User authenticated, redirecting to:', redirectTo || '/');
      isRedirectingRef.current = true;
      router.push(redirectTarget);
      return;
    }

    // Only trigger sign-in ONCE when keycloak is ready and not authenticated
    if (!code && keycloak && !keycloak.authenticated && !hasTriggeredRef.current && !isRedirectingRef.current) {
      console.log('[SignInClient] Triggering Keycloak sign-in (ONCE)...');
      hasTriggeredRef.current = true;

      // Sign in with Keycloak
      keycloak.login({ redirectUri: `${window.location.origin}/auth/signin?callbackUrl=${encodeURIComponent(redirectTarget)}` });
    }
  }, [code, keycloak, redirectTo, router]);

  const resolve = (key: string, fallback: string) => {
    const value = t(key);
    return value === key ? fallback : value;
  };

  const statusMessage = (() => {
    if (isExchanging) {
      return resolve('auth.establishingSession', 'Establishing secure session…');
    }
    if (exchangeFailed) {
      return resolve('auth.sessionFailed', 'Unable to establish session, redirecting…');
    }
    if (!keycloak) {
      return resolve('auth.checkingAuth', 'Checking authentication status…');
    }
    if (keycloak && !keycloak.authenticated && !code) {
      return resolve('auth.redirectingSignIn', 'Redirecting to sign-in…');
    }
    return resolve('auth.redirectingHome', 'Redirecting to home…');
  })();

  return (
    <Container>
      <Box sx={{ mt: 6 }}>
        <Typography variant="h6">{statusMessage}</Typography>
        {process.env.NODE_ENV !== 'production' && (
          <Typography variant="caption" sx={{ mt: 2, display: 'block', color: 'text.secondary' }}>
            Authenticated: {String(keycloak?.authenticated)} | Triggered: {hasTriggeredRef.current.toString()} | Redirecting: {isRedirectingRef.current.toString()}
          </Typography>
        )}
      </Box>
    </Container>
  );
}
