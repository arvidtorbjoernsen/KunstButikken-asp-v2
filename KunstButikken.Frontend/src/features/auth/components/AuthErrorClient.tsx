"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { useKeycloak } from "@/features/auth/lib/keycloak";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import Link from "next/link";

export default function AuthErrorClient({ error }: { error?: string | null }) {
  const { t } = useTranslations();
  const { keycloak } = useKeycloak();

  const handleLogin = () => {
    keycloak?.login();
  };

  const handleRegister = () => {
    keycloak?.register();
  };

  return (
    <Container sx={{ py: 4 }}>
      <Typography variant="h4" gutterBottom>
        {t("auth.errorTitle")}
      </Typography>
      <Alert severity="error" sx={{ mb: 2 }}>
        {error || t("auth.errorTitle")}
      </Alert>
      <Button onClick={handleLogin} variant="contained" sx={{ mr: 1 }}>
        {t("auth.tryAgain")}
      </Button>
      <Button onClick={handleRegister} variant="outlined" sx={{ mr: 1 }}>
        {t("auth.registerAction")}
      </Button>
      <Button component={Link} href="/" variant="text" sx={{ mr: 1 }}>
        {t("auth.backHome")}
      </Button>
      {process.env.NODE_ENV !== "production" && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          Dev help: Ensure your Keycloak configuration is correct.
        </Typography>
      )}
    </Container>
  );
}
