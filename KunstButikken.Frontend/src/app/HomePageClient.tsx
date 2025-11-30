"use client";

import React from "react";
import ArtCard from "@/features/art/components/ArtCard";
import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import type { UiArt } from "@/features/art/types/art";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Grid from "@mui/material/Grid";
import Typography from "@mui/material/Typography";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Divider from "@mui/material/Divider";
import AuctionCard from '@/features/auction/components/AuctionCard';
import type { UiAuction } from '@/features/auction/types/auction';

interface HomePageClientProps {
  initialFeatured: UiArt[];
  initialAll?: UiArt[];
  sellerFeatured?: UiArt[];
  sellerArt?: UiArt[];
  sellerAuctions?: UiAuction[];
  isSeller?: boolean;
}

const SELLER_VIEW: 'mine' | 'all' = 'mine';
const PUBLIC_VIEW: 'mine' | 'all' = 'all';

/**
 * Client-side home page component
 * Receives server-rendered data and provides interactivity
 * Similar to Angular components that receive data from resolvers
 */
export default function HomePageClient({
  initialFeatured,
  initialAll = [],
  sellerFeatured = [],
  sellerArt = [],
  sellerAuctions = [],
  isSeller = false,
}: HomePageClientProps) {
  const { t } = useTranslations();
  const [viewMode, setViewMode] = React.useState<'mine' | 'all'>(isSeller ? SELLER_VIEW : PUBLIC_VIEW);

  const handleViewModeChange = (_event: React.MouseEvent<HTMLElement>, next: 'mine' | 'all' | null) => {
    if (next) {
      setViewMode(next);
    }
  };

  const showingMine = isSeller && viewMode === SELLER_VIEW;
  const featuredList = showingMine ? sellerFeatured : initialFeatured;
  const catalogList = showingMine ? sellerArt : initialAll;
  const hasFeatured = (featuredList?.length ?? 0) > 0;
  const hasCatalog = (catalogList?.length ?? 0) > 0;
  const shouldShowAuctions = showingMine && isSeller;
  const hasAuctions = shouldShowAuctions && (sellerAuctions?.length ?? 0) > 0;

  const renderArtGrid = React.useCallback(
    (items: UiArt[], showStatus = false) => (
      <Grid container spacing={3}>
        {items.map(art => (
          <Grid size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }} key={art.id}>
            <ArtCard art={art} showStatus={showStatus} alwaysShowFeatured={showingMine} />
          </Grid>
        ))}
      </Grid>
    ),
    [showingMine],
  );

  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
      <Box mb={6}>
        <Typography variant="h4" component="h1" gutterBottom>
          {t("home.title")}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t("home.subtitle")}
        </Typography>
        {isSeller && (
          <Box mt={3}>
            <ToggleButtonGroup value={viewMode} exclusive onChange={handleViewModeChange} aria-label={t('home.viewToggleLabel')}>
              <ToggleButton value="mine" aria-label={t('home.viewMine')}>
                {t('home.viewMine')}
              </ToggleButton>
              <ToggleButton value="all" aria-label={t('home.viewAll')}>
                {t('home.viewAll')}
              </ToggleButton>
            </ToggleButtonGroup>
          </Box>
        )}
      </Box>

      {shouldShowAuctions && (
        <Box mb={4}>
          <Typography variant="h6" gutterBottom>
            {t('home.myAuctions')}
          </Typography>
          {hasAuctions ? (
            <Grid container spacing={3}>
              {sellerAuctions.map(auction => (
                <Grid size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }} key={auction.id}>
                  <AuctionCard auction={auction} showStatus />
                </Grid>
              ))}
            </Grid>
          ) : (
            <Typography variant="body2" color="text.secondary">
              {t('home.noAuctions')}
            </Typography>
          )}
          <Divider sx={{ mt: 4, mb: 4 }} />
        </Box>
      )}

      <Box mb={4}>
        <Typography variant="h6" gutterBottom>
          {showingMine ? t('home.myFeatured') : t('home.featured')}
        </Typography>
        {hasFeatured ? renderArtGrid(featuredList, showingMine) : (
          <Typography variant="body2" color="text.secondary">{t('home.noFeatured')}</Typography>
        )}
      </Box>

      <Box mb={4}>
        <Typography variant="h6" gutterBottom>
          {showingMine ? t('home.myCatalogue') : t('home.allArt')}
        </Typography>
        {hasCatalog ? renderArtGrid(catalogList, showingMine) : (
          <Typography variant="body2" color="text.secondary">{t('home.noArtwork')}</Typography>
        )}
      </Box>
    </Container>
  );
}
