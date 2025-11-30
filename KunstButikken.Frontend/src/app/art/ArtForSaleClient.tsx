"use client";
import React from 'react';
import Container from '@mui/material/Container';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid';
import Divider from '@mui/material/Divider';
import ArtCard from '@/features/art/components/ArtCard';
import AuctionCard from '@/features/auction/components/AuctionCard';
import type { UiArt } from '@/features/art/types/art';
import type { UiAuction } from '@/features/auction/types/auction';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';

export default function ArtForSaleClient({
  myAuctions,
  myArt,
  others,
  isSeller,
  isBuyer,
}: {
  myAuctions?: UiAuction[];
  myArt?: UiArt[];
  others: UiArt[];
  isSeller?: boolean;
  isBuyer?: boolean;
}) {
  const { t } = useTranslations();

  const sortArtByFeatured = React.useCallback((items?: UiArt[]) => {
    return (items ?? [])
      .slice()
      .sort((a, b) => Number(Boolean(b.isFeatured)) - Number(Boolean(a.isFeatured)));
  }, []);

  const sortedMyArt = React.useMemo(() => sortArtByFeatured(myArt), [myArt, sortArtByFeatured]);
  const sortedOthers = React.useMemo(() => sortArtByFeatured(others), [others, sortArtByFeatured]);
  const sellerHasAuctions = (myAuctions?.length ?? 0) > 0;
  const sellerHasArt = sortedMyArt.length > 0;
  const hasPublicArt = sortedOthers.length > 0;

  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
      <Box mb={6}>
        <Typography variant="h4" component="h1" gutterBottom>
          {t('artPage.title')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {isSeller ? t('artPage.sellerDescription') : isBuyer ? t('artPage.buyerDescription') : t('artPage.defaultDescription')}
        </Typography>
      </Box>

      {isSeller && (
        <Box mb={6}>
          <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, mb: 3 }}>
            {t('auction.yourAuctions')}
          </Typography>
          {sellerHasAuctions ? (
            <Grid container spacing={3}>
              {myAuctions?.map(auction => (
                <Grid key={auction.id} size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }}>
                  <AuctionCard auction={auction} showStatus />
                </Grid>
              ))}
            </Grid>
          ) : (
            <Typography variant="body2" color="text.secondary">
              {t('auction.none')}
            </Typography>
          )}
          <Divider sx={{ mt: 6 }} />
        </Box>
      )}

      {isSeller && (
        <Box mb={6}>
          <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, mb: 3 }}>
            {t('artPage.yourArt')}
          </Typography>
          {sellerHasArt ? (
            <Grid container spacing={3}>
              {sortedMyArt.map(a => (
                <Grid key={a.id} size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }}>
                  <ArtCard art={a} showStatus alwaysShowFeatured />
                </Grid>
              ))}
            </Grid>
          ) : (
            <Typography variant="body2" color="text.secondary">
              {t('artPage.noArt') ?? 'No art found.'}
            </Typography>
          )}
          <Divider sx={{ mt: 6, mb: 4 }} />
        </Box>
      )}

      <Box>
        {isSeller && (
          <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, mb: 3 }}>
            {t('artPage.otherArt')}
          </Typography>
        )}
        {!hasPublicArt ? (
          <Typography variant="body2" color="text.secondary">
            {t('artPage.noArt') ?? 'No art found.'}
          </Typography>
        ) : (
          <Grid container spacing={3}>
            {sortedOthers.map(a => (
              <Grid key={a.id} size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }}>
                <ArtCard art={a} alwaysShowFeatured />
              </Grid>
            ))}
          </Grid>
        )}
      </Box>
    </Container>
  );
}
