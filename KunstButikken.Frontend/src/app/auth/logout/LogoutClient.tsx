"use client";

import { useKeycloak } from "@/features/auth/lib/keycloak";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useEffect, useState } from "react";

interface LogoutClientProps {
  keycloakIssuer: string | null | undefined;
  idToken?: string;
}

export default function LogoutClient({ keycloakIssuer, idToken }: LogoutClientProps) {
  const { keycloak } = useKeycloak();
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string>("Initializing...");
  const [hasIdToken, setHasIdToken] = useState<boolean | null>(null);

  useEffect(() => {
    const performLogout = async () => {
      try {
        // Clear storage first
        try {
          localStorage.clear();
          console.log("[Logout] Cleared localStorage");
        } catch (e) {
          console.error("[Logout] Failed to clear localStorage:", e);
        }
        try {
          sessionStorage.clear();
          console.log("[Logout] Cleared sessionStorage");
        } catch (e) {
          console.error("[Logout] Failed to clear sessionStorage:", e);
        }

        setStatus("Starting logout process");
        console.log('[Logout] Starting logout process');

        // Build logout URL
        setStatus("Preparing end-session URL...");
        const issuerBase = keycloakIssuer ?? (process.env.NEXT_PUBLIC_KEYCLOAK_BASE_URL
          ? `${process.env.NEXT_PUBLIC_KEYCLOAK_BASE_URL}/realms/${process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'kunstbutikken'}`
          : undefined);

        if (!issuerBase) {
          // Cannot build logout URL without issuer; redirect home as a fallback
          setStatus('Keycloak issuer not configured; redirecting home');
          setTimeout(() => { window.location.href = '/'; }, 500);
          return;
        }

        const base = `${issuerBase}/protocol/openid-connect/logout`;
        const returnUrl = `${window.location.origin}/`;
        const params = new URLSearchParams({ post_logout_redirect_uri: returnUrl });

        const hasIdToken = !!idToken;
        setHasIdToken(hasIdToken);

        if (idToken) {
          params.set('id_token_hint', idToken);
        } else {
          params.set('client_id', 'kunstbutikken-frontend');
        }

        const logoutUrl = `${base}?${params.toString()}`;

        await new Promise(resolve => setTimeout(resolve, 300));

        // Use Keycloak client to logout
        setStatus(hasIdToken ? "Using id_token_hint for Keycloak logout" : "Using client_id fallback for Keycloak logout");
        console.log('[Logout] Logging out of Keycloak');

        if (keycloak) {
          keycloak.logout({ redirectUri: returnUrl });
        } else {
          // Fallback if keycloak instance not ready
          console.log('[Logout] Redirecting to logout URL:', logoutUrl);
          window.location.href = logoutUrl;
        }
      } catch (err) {
        console.error('[Logout] Error during logout:', err);
        setError(err instanceof Error ? err.message : 'Logout failed');
        setStatus("Error during logout");
        setTimeout(() => {
          window.location.href = '/';
        }, 3000);
      }
    };

    performLogout();
  }, [keycloakIssuer, idToken, keycloak]);

  return (
    <Container>
      <Box sx={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", minHeight: "60vh", gap: 3 }}>
        {error ? (
          <>
            <Alert severity="error" sx={{ width: '100%', maxWidth: 600 }}>
              <Typography variant="h6">Logout Error</Typography>
              <Typography variant="body2">{error}</Typography>
            </Alert>
            <Typography variant="caption">
              Redirecting to home page...
            </Typography>
          </>
        ) : (
          <>
            <CircularProgress size={60} />
            <Stack spacing={2} sx={{ width: '100%', maxWidth: 600 }}>
              <Alert severity="info">
                <Typography variant="h6" gutterBottom>Logging out...</Typography>
                <Typography variant="body2">
                  <strong>Status:</strong> {status}
                </Typography>
              </Alert>

              {hasIdToken !== null && (
                <Alert severity={hasIdToken ? "success" : "warning"}>
                  <Typography variant="body2">
                    <strong>ID Token Available:</strong> {hasIdToken ? "Yes ✓" : "No - Using fallback"}
                  </Typography>
                  <Typography variant="caption" display="block" sx={{ mt: 1 }}>
                    {hasIdToken
                      ? "Logout will properly clear Keycloak SSO session"
                      : "Using client_id fallback - may not fully clear SSO"}
                  </Typography>
                </Alert>
              )}
            </Stack>

            <Typography variant="caption" color="text.secondary" sx={{ mt: 2 }}>
              You will be redirected automatically
            </Typography>
          </>
        )}
      </Box>
    </Container>
  );
}
