"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import LinkButton from "@/features/navigation/components/LinkButton";
import { useKeycloak } from "@/features/auth/lib/keycloak";
import AddPhotoAlternateIcon from "@mui/icons-material/AddPhotoAlternate";
import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import LoginIcon from "@mui/icons-material/Login";
import LogoutIcon from "@mui/icons-material/Logout";
import PersonAddIcon from "@mui/icons-material/PersonAdd";
import Button from "@mui/material/Button";

export type NavbarAuthActionsKeycloakProps = {
  isAuthed: boolean;
  isSellerVerified: boolean; // Added new prop
};

/**
 * Navbar auth actions using Keycloak
 * Similar to Angular's navbar auth component using AuthService
 */
export default function NavbarAuthActionsKeycloak({ isAuthed, isSellerVerified }: NavbarAuthActionsKeycloakProps) {
  const { t } = useTranslations();
  const { login, logout, isAdmin } = useKeycloak();
  const userIsAdmin = isAuthed && isAdmin;

  console.log('[NavbarAuthActionsKeycloak] isAuthed:', isAuthed);
  console.log('[NavbarAuthActionsKeycloak] isAdmin:', isAdmin);
  console.log('[NavbarAuthActionsKeycloak] userIsAdmin:', userIsAdmin);
  console.log('[NavbarAuthActionsKeycloak] isSellerVerified:', isSellerVerified); // Log new prop

  if (!isAuthed) {
    return (
      <>
        <Button
          onClick={login}
          size="small"
          color="inherit"
          startIcon={<LoginIcon />}
          sx={{ display: { xs: "none", md: "inline-flex" } }}
        >
          {t("nav.signin")}
        </Button>
        <Button
          onClick={login}
          size="small"
          variant="outlined"
          startIcon={<PersonAddIcon />}
          sx={{ ml: { xs: 0.5, md: 1 } }}
        >
          {t("nav.register")}
        </Button>
      </>
    );
  }

  return (
    <>
      {userIsAdmin && (
        <LinkButton
          href="/admin"
          size="small"
          color="secondary"
          variant="contained"
          startIcon={<AdminPanelSettingsIcon />}
          sx={{ display: { xs: "none", md: "inline-flex" }, minWidth: 56, px: 1 }}
        >
          {t("nav.admin")}
        </LinkButton>
      )}
      {isSellerVerified && ( // Conditionally render based on isSellerVerified
        <LinkButton
          href="/art/sell/new-art"
          size="small"
          color="inherit"
          startIcon={<AddPhotoAlternateIcon />}
          sx={{ display: { xs: "none", md: "inline-flex" }, minWidth: 56, px: 1 }}
        >
          {t("nav.sellArt")}
        </LinkButton>
      )}
      <LinkButton href="/auth/me" size="small" variant="outlined" sx={{ ml: { xs: 0.5, md: 1 } }}>
        {t("nav.profile")}
      </LinkButton>
      <Button
        onClick={logout}
        size="small"
        color="inherit"
        startIcon={<LogoutIcon />}
        sx={{ ml: { xs: 0.5, md: 1 } }}
      >
        {t("nav.logout")}
      </Button>
    </>
  );
}
