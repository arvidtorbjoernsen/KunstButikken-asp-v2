"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import ThemeToggle from "@/features/ui/components/ThemeToggle";
import { useKeycloak } from "@/features/auth/lib/keycloak";
import AddPhotoAlternateIcon from "@mui/icons-material/AddPhotoAlternate";
import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import BrushIcon from "@mui/icons-material/Brush";
import GavelIcon from "@mui/icons-material/Gavel";
import LoginIcon from "@mui/icons-material/Login";
import LogoutIcon from "@mui/icons-material/Logout";
import MenuIcon from "@mui/icons-material/Menu";
import PaletteIcon from "@mui/icons-material/Palette";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Divider from "@mui/material/Divider";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import Link from "next/link";
import { useCallback, useState } from "react";

export type MobileMenuKeycloakProps = {
  isAuthed?: boolean;
  isSellerVerified: boolean; // Added new prop
};

/**
 * Mobile menu using Keycloak authentication
 * Similar to Angular's mobile menu using AuthService
 */
export default function MobileMenuKeycloak({ isAuthed, isSellerVerified }: MobileMenuKeycloakProps) {
  const [open, setOpen] = useState(false);
  const toggle = useCallback((val: boolean) => () => setOpen(val), []);
  const { t } = useTranslations();
  const { login, logout, isAdmin } = useKeycloak();
  const userIsAdmin = isAuthed && isAdmin;

  console.log('[MobileMenuKeycloak] isAuthed:', isAuthed);
  console.log('[MobileMenuKeycloak] isAdmin:', isAdmin);
  console.log('[MobileMenuKeycloak] userIsAdmin:', userIsAdmin);
  console.log('[MobileMenuKeycloak] isSellerVerified:', isSellerVerified); // Log new prop

  const handleLogin = (target?: string) => {
    login({ redirectUri: `${window.location.origin}/auth/signin?callbackUrl=${encodeURIComponent(target ?? '/')}` });
  };

  const handleLogout = () => {
    setOpen(false);
    logout();
  };

  return (
    <>
      <IconButton
        color="inherit"
        onClick={toggle(true)}
        aria-label={t("nav.menu")}
        sx={{ display: { xs: "inline-flex", md: "none" }, ml: "auto" }}
      >
        <MenuIcon />
      </IconButton>
      <Drawer anchor="right" open={open} onClose={toggle(false)}>
        <Box sx={{ width: 280, p: 2, display: "flex", flexDirection: "column", gap: 1 }} role="presentation" onClick={toggle(false)}>
          <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: 1 }}>
            <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
              <PaletteIcon sx={{ fontSize: 24 }} />
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                KunstButikken
              </Typography>
            </Box>
            <ThemeToggle />
          </Box>
          <Divider />
          <List sx={{ p: 0 }}>
            <ListItem disablePadding>
              <Tooltip title={t("nav.art")} placement="left">
                <ListItemButton component={Link} href="/art">
                  <ListItemIcon sx={{ minWidth: 40 }}>
                    <BrushIcon />
                  </ListItemIcon>
                  <ListItemText primary={t("nav.art")} />
                </ListItemButton>
              </Tooltip>
            </ListItem>
            <ListItem disablePadding>
              <Tooltip title={t("nav.auctions")} placement="left">
                <ListItemButton component={Link} href="/art/auctions">
                  <ListItemIcon sx={{ minWidth: 40 }}>
                    <GavelIcon />
                  </ListItemIcon>
                  <ListItemText primary={t("nav.auctions")} />
                </ListItemButton>
              </Tooltip>
            </ListItem>
            {isAuthed && (
              <>
                {userIsAdmin && (
                  <ListItem disablePadding>
                    <Tooltip title={t("nav.admin")} placement="left">
                      <ListItemButton component={Link} href="/admin">
                        <ListItemIcon sx={{ minWidth: 40 }}>
                          <AdminPanelSettingsIcon color="secondary" />
                        </ListItemIcon>
                        <ListItemText primary={t("nav.admin")} />
                      </ListItemButton>
                    </Tooltip>
                  </ListItem>
                )}
                {isSellerVerified && ( // Conditionally render based on isSellerVerified
                  <ListItem disablePadding>
                    <Tooltip title={t("nav.sellArt")} placement="left">
                      <ListItemButton component={Link} href="/art/sell/new-art">
                        <ListItemIcon sx={{ minWidth: 40 }}>
                          <AddPhotoAlternateIcon />
                        </ListItemIcon>
                        <ListItemText primary={t("nav.sellArt")} />
                      </ListItemButton>
                    </Tooltip>
                  </ListItem>
                )}
              </>
            )}
          </List>
          <Divider sx={{ my: 1 }} />
          {!isAuthed ? (
            <>
              <Button
                onClick={() => handleLogin('/auth/me')}
                variant="outlined"
                fullWidth
                startIcon={<LoginIcon />}
              >
                {t("nav.signin")}
              </Button>
              <Button
                onClick={() => handleLogin('/auth/me')}
                variant="contained"
                fullWidth
              >
                {t("nav.register")}
              </Button>
            </>
          ) : (
            <>
              <Button
                component={Link}
                href="/auth/me"
                variant="outlined"
                fullWidth
              >
                {t("nav.profile")}
              </Button>
              <Button
                onClick={handleLogout}
                variant="outlined"
                color="inherit"
                fullWidth
                startIcon={<LogoutIcon />}
              >
                {t("nav.logout")}
              </Button>
            </>
          )}
        </Box>
      </Drawer>
    </>
  );
}
