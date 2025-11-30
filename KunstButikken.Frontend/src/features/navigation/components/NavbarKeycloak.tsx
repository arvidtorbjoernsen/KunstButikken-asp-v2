"use client";

import React, { useState, useRef } from "react"; // Import useState and useRef
import AppBar from "@mui/material/AppBar";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Toolbar from "@mui/material/Toolbar";
import IconButton from "@mui/material/IconButton"; // Import IconButton
import SearchIcon from "@mui/icons-material/Search"; // Import SearchIcon

import DevCheckButton from "@/features/navigation/components/DevCheckButton";
import MobileMenuKeycloak from "@/features/navigation/components/MobileMenuKeycloak";
import NavbarAuthActionsKeycloak from "@/features/navigation/components/NavbarAuthActionsKeycloak";
import NavbarBrand from "@/features/navigation/components/NavbarBrand";
import NavbarLanguageMenu from "@/features/navigation/components/NavbarLanguageMenu";
import NavbarLinks from "@/features/navigation/components/NavbarLinks";
import SearchOverlay from "@/features/navigation/components/SearchOverlay"; // Import SearchOverlay
import ThemeToggle from "@/features/ui/components/ThemeToggle";
import { useKeycloak } from "@/features/auth/lib/keycloak";
import { tokens } from "@/features/ui/theme/tokens";
import { useTranslations } from "@/features/i18n/components/TranslationProvider"; // Import useTranslations

/**
 * Client-side Navbar using Keycloak authentication
 * Similar to Angular's navbar component that uses AuthService
 */
export default function NavbarKeycloak() {
  const { t } = useTranslations();
  const { authenticated, loading, isSeller } = useKeycloak(); // Destructure isSeller

  // During loading, show navbar but without auth state
  const isAuthed = !loading && authenticated;

  const [searchOverlayOpen, setSearchOverlayOpen] = useState(false);
  const toolbarRef = useRef<HTMLDivElement>(null); // Ref for the Toolbar

  const handleOpenSearchOverlay = () => {
    setSearchOverlayOpen(true);
    // Set anchorEl to the Toolbar itself for centering the Popover below it
    setAnchorEl(toolbarRef.current);
  };

  const handleCloseSearchOverlay = () => {
    setSearchOverlayOpen(false);
    setAnchorEl(null); // Clear anchorEl when closing
  };

  // State for anchorEl, initialized to null
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);


  return (
    <AppBar
      position="sticky"
      color="default"
      elevation={0}
      sx={{
        bgcolor: "background.paper",
        color: "text.primary",
        borderBottom: "1px solid",
        borderColor: "divider",
      }}
    >
      <Toolbar ref={toolbarRef} disableGutters sx={{ minHeight: { xs: tokens.layout.navHeight.xs, sm: tokens.layout.navHeight.sm } }}> {/* Attach ref here */}
        <Container maxWidth="xl" sx={{ px: { xs: 2, sm: 2 } }}>
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              gap: { xs: tokens.space.md, md: tokens.space.lg },
              flexWrap: "nowrap",
              minWidth: 0,
            }}
          >
            {/* LEFT: Brand */}
            <NavbarBrand />

            {/* CENTER: Primary links (hidden on small) */}
            <NavbarLinks />

            {/* Search Toggle Icon (visible on all screens) */}
            <Box sx={{ ml: { xs: "auto", lg: 0 }, flexShrink: 0 }}> {/* Adjusted margin for positioning */}
              <IconButton
                color="inherit"
                aria-label={t("aria.openSearch")} // Use translation for aria-label
                onClick={handleOpenSearchOverlay}
              >
                <SearchIcon />
              </IconButton>
            </Box>

            {/* RIGHT: Actions */}
            <Box sx={{ display: "flex", alignItems: "center", gap: { xs: 0.5, md: 1 }, minWidth: 0, flexShrink: 0, whiteWhiteSpace: "nowrap" }}>
              <DevCheckButton />
              <NavbarLanguageMenu />
              <ThemeToggle />

              <Box sx={{ display: { xs: "none", md: "flex" }, alignItems: "center", gap: { xs: 0.5, md: 1 } }}>
                <NavbarAuthActionsKeycloak isAuthed={isAuthed} isSellerVerified={isSeller} /> {/* Pass isSeller as isSellerVerified */}
              </Box>

              {/* Mobile: Drawer menu - positioned at far right */}
              <MobileMenuKeycloak isAuthed={isAuthed} isSellerVerified={isSeller} /> {/* Pass isSeller to MobileMenuKeycloak as well */}
            </Box>
          </Box>
        </Container>
      </Toolbar>

      {/* Search Overlay */}
      <SearchOverlay open={searchOverlayOpen} onClose={handleCloseSearchOverlay} anchorEl={anchorEl} />
    </AppBar>
  );
}
