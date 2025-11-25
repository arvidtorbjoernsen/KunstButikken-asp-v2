"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { useKeycloak } from "@/features/auth/lib/keycloak";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { useEffect, useRef } from "react";

export default function SignInClient({ redirectTo }: { redirectTo?: string }) {
  const { t } = useTranslations();
  const { keycloak } = useKeycloak();
  const router = useRouter();
  const hasTriggeredRef = useRef(false);
  const isRedirectingRef = useRef(false);

  useEffect(() => {
    // If already authenticated, redirect to the intended destination (ONCE)
    if (keycloak?.authenticated && !isRedirectingRef.current) {
      console.log('[SignInClient] User authenticated, redirecting to:', redirectTo || '/');
      isRedirectingRef.current = true;
      router.push(redirectTo || "/");
      return;
    }

    // Only trigger sign-in ONCE when keycloak is ready and not authenticated
    if (keycloak && !keycloak.authenticated && !hasTriggeredRef.current && !isRedirectingRef.current) {
      console.log('[SignInClient] Triggering Keycloak sign-in (ONCE)...');
      hasTriggeredRef.current = true;

      // Sign in with Keycloak
      keycloak.login({ redirectUri: `${window.location.origin}${redirectTo || '/'}` });
    }
  }, [keycloak, redirectTo, router]);

  return (
    <Container>
      <Box sx={{ mt: 6 }}>
        <Typography variant="h6">
          {!keycloak && t("auth.checkingAuth")}
          {keycloak && !keycloak.authenticated && t("auth.redirectingSignIn")}
          {keycloak?.authenticated && t("auth.redirectingHome")}
        </Typography>
        {process.env.NODE_ENV !== 'production' && (
          <Typography variant="caption" sx={{ mt: 2, display: 'block', color: 'text.secondary' }}>
            Authenticated: {String(keycloak?.authenticated)} | Triggered: {hasTriggeredRef.current.toString()} | Redirecting: {isRedirectingRef.current.toString()}
          </Typography>
        )}
      </Box>
    </Container>
  );
}
