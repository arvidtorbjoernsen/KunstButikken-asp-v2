"use client";

import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import ThemeToggle from "@/features/ui/components/ThemeToggle";
import AddPhotoAlternateIcon from "@mui/icons-material/AddPhotoAlternate";
import BrushIcon from "@mui/icons-material/Brush";
import GavelIcon from "@mui/icons-material/Gavel";
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
import { useRouter } from "next/navigation";
import { useCallback, useState } from "react";

export type MobileMenuProps = {
  isAuthed?: boolean;
  isSeller?: boolean;
};

export default function MobileMenu({ isAuthed, isSeller }: MobileMenuProps) {
  const [open, setOpen] = useState(false);
  const toggle = useCallback((val: boolean) => () => setOpen(val), []);
  const { t } = useTranslations();
  const router = useRouter();

  const handleLogout = () => {
    setOpen(false);
    router.push('/auth/logout');
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
                <ListItemButton onClick={() => { router.push('/art'); setOpen(false); }}>
                  <ListItemIcon sx={{ minWidth: 40 }}>
                    <BrushIcon />
                  </ListItemIcon>
                  <ListItemText primary={t("nav.art")} />
                </ListItemButton>
              </Tooltip>
            </ListItem>
            <ListItem disablePadding>
              <Tooltip title={t("nav.auctions")} placement="left">
                <ListItemButton onClick={() => { router.push('/art/auctions'); setOpen(false); }}>
                  <ListItemIcon sx={{ minWidth: 40 }}>
                    <GavelIcon />
                  </ListItemIcon>
                  <ListItemText primary={t("nav.auctions")} />
                </ListItemButton>
              </Tooltip>
            </ListItem>
          </List>
          {isSeller ? (
            <>
              <Divider />
              <List sx={{ p: 0 }}>
                <ListItem disablePadding>
                  <ListItemButton component={Link} href="/art/sell/new-art">
                    <ListItemIcon sx={{ minWidth: 40 }}>
                      <AddPhotoAlternateIcon />
                    </ListItemIcon>
                    <ListItemText primary={t("nav.sellArt")} />
                  </ListItemButton>
                </ListItem>
              </List>
            </>
          ) : null}
          <Divider />
          {!isAuthed ? (
            <Box sx={{ display: "flex", gap: 1, mt: 1 }}>
              <Button fullWidth component={Link} href="/auth/signin" variant="outlined">
                {t("nav.signin")}
              </Button>
              <Button fullWidth component={Link} href="/auth/register" variant="contained">
                {t("nav.register")}
              </Button>
            </Box>
          ) : (
            <Box sx={{ display: "flex", flexDirection: "column", gap: 1, mt: 1 }}>
              <Button fullWidth component={Link} href="/auth/me" variant="contained">
                {t("nav.profile")}
              </Button>
              <Button fullWidth onClick={handleLogout} variant="outlined" color="error" startIcon={<LogoutIcon />}>
                {t("nav.logout")}
              </Button>
            </Box>
          )}
        </Box>
      </Drawer>
    </>
  );
}
