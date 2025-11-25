"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { useKeycloak } from "@/features/auth/lib/keycloak";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import { useRouter } from "next/navigation";
import { useEffect, useRef } from "react";

export default function RegisterClient({ redirectTo }: { redirectTo?: string }) {
  const { t } = useTranslations();
  const { keycloak } = useKeycloak();
  const router = useRouter();
  const hasTriggeredSignIn = useRef(false);

  useEffect(() => {
    // If already authenticated, redirect to the intended destination
    if (keycloak?.authenticated) {
      router.push(redirectTo || "/");
      return;
    }

    // Only trigger register once to avoid multiple redirects
    if (keycloak && !keycloak.authenticated && !hasTriggeredSignIn.current) {
      hasTriggeredSignIn.current = true;
      // Trigger Keycloak registration
      keycloak.register({ redirectUri: `${window.location.origin}${redirectTo || '/'}` });
    }
  }, [keycloak, redirectTo, router]);

  return (
    <Container>
      <Box sx={{ mt: 6 }}>
        <Typography variant="h6">{t("auth.redirectingRegister")}</Typography>
      </Box>
    </Container>
  );
}
