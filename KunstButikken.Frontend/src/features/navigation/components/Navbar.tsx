'use client';

import React, { useState } from 'react'; // Import useState
import AppBar from '@mui/material/AppBar';
import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Toolbar from '@mui/material/Toolbar';
import IconButton from '@mui/material/IconButton'; // Import IconButton
import SearchIcon from '@mui/icons-material/Search'; // Import SearchIcon

import DevCheckButton from '@/features/navigation/components/DevCheckButton';
import MobileMenu from '@/features/navigation/components/MobileMenu';
import NavbarAuthActions from '@/features/navigation/components/NavbarAuthActions';
import NavbarBrand from '@/features/navigation/components/NavbarBrand';
import NavbarLanguageMenu from '@/features/navigation/components/NavbarLanguageMenu';
import NavbarLinks from '@/features/navigation/components/NavbarLinks';
// import NavbarSearch from "@/features/navigation/components/NavbarSearch"; // Remove NavbarSearch import
import SearchOverlay from '@/features/navigation/components/SearchOverlay'; // Import SearchOverlay
import ThemeToggle from '@/features/ui/components/ThemeToggle';
import { useKeycloak } from '@/features/auth/lib/keycloak';
import { tokens } from '@/features/ui/theme/tokens';
import type { KeycloakTokenParsed } from '@/features/auth/types/keycloak';
import { useTranslations } from '@/features/i18n/components/TranslationProvider'; // Import useTranslations

// Lightweight client-side role derivation from Keycloak token
function deriveRoles(tokenParsed: KeycloakTokenParsed | undefined) {
  const roles: string[] = tokenParsed?.realm_access?.roles ?? [];
  const isSeller = roles.some(r => r === 'Seller' || r === 'seller');
  return { roles, isSeller };
}

export default function Navbar() {
  const { t } = useTranslations(); // Use translations for aria-label
  const { authenticated, keycloak, loading } = useKeycloak();
  const isAuthed = authenticated && !loading;
  const { isSeller } = deriveRoles(keycloak?.tokenParsed);

  const [searchOverlayOpen, setSearchOverlayOpen] = useState(false);
  const [searchAnchorEl, setSearchAnchorEl] = useState<HTMLElement | null>(null);

  const handleOpenSearchOverlay = (e: React.MouseEvent<HTMLElement>) => {
    setSearchAnchorEl(e.currentTarget);
    setSearchOverlayOpen(true);
  };

  const handleCloseSearchOverlay = () => {
    setSearchOverlayOpen(false);
    setSearchAnchorEl(null);
  };

  return (
    <AppBar
      position="sticky"
      color="default"
      elevation={0}
      sx={{
        bgcolor: 'background.paper',
        color: 'text.primary',
        borderBottom: '1px solid',
        borderColor: 'divider',
      }}
    >
      <Toolbar
        disableGutters
        sx={{ minHeight: { xs: tokens.layout.navHeight.xs, sm: tokens.layout.navHeight.sm } }}
      >
        <Container maxWidth="xl" sx={{ px: { xs: 2, sm: 2 } }}>
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: { xs: tokens.space.md, md: tokens.space.lg },
              flexWrap: 'nowrap',
              minWidth: 0,
            }}
          >
            {/* LEFT: Brand */}
            <NavbarBrand />

            {/* CENTER: Primary links (hidden on small) */}
            <NavbarLinks />

            {/* Search Toggle Icon (visible on all screens) */}
            <Box sx={{ ml: { xs: 'auto', lg: 0 }, flexShrink: 0 }}>
              {' '}
              {/* Adjusted margin for positioning */}
              <IconButton
                color="inherit"
                aria-label={t('aria.openSearch')} // Use translation for aria-label
                onClick={handleOpenSearchOverlay}
              >
                <SearchIcon />
              </IconButton>
            </Box>

            {/* RIGHT: Actions */}
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: { xs: 0.5, md: 1 },
                minWidth: 0,
                flexShrink: 0,
                whiteSpace: 'nowrap',
              }}
            >
              <DevCheckButton />
              <NavbarLanguageMenu />
              <ThemeToggle />

              <Box
                sx={{
                  display: { xs: 'none', md: 'flex' },
                  alignItems: 'center',
                  gap: { xs: 0.5, md: 1 },
                }}
              >
                <NavbarAuthActions isAuthed={isAuthed} isSeller={isSeller} />
              </Box>

              {/* Mobile: Drawer menu - positioned at far right */}
              <MobileMenu isAuthed={isAuthed} isSeller={isSeller} />
            </Box>
          </Box>
        </Container>
      </Toolbar>

      {/* Search Overlay */}
      <SearchOverlay
        open={searchOverlayOpen}
        onClose={handleCloseSearchOverlay}
        anchorEl={searchAnchorEl}
      />
    </AppBar>
  );
}
