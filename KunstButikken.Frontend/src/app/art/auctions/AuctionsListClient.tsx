'use client';

import Container from '@mui/material/Container';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid';
import Divider from '@mui/material/Divider';
import AuctionCard from '@/features/auction/components/AuctionCard';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import type { UiAuction } from '@/features/auction/types/auction';

export default function AuctionsListClient({
  auctions,
  sellerAuctions = [],
  isSeller,
  isBuyer,
}: {
  auctions: UiAuction[];
  sellerAuctions?: UiAuction[];
  isSeller?: boolean;
  isBuyer?: boolean;
}) {
  const { t } = useTranslations();
  // Data is already filtered server-side; just render
  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
      <Box mb={6}>
        <Typography variant="h4" component="h1" gutterBottom>
          {t('nav.auctions')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {isSeller
            ? t('auction.sellerAuctionsSubtitle')
            : isBuyer
              ? t('auction.buyerAuctionsSubtitle')
              : t('auction.subtitle')}
        </Typography>
      </Box>
      {isSeller && sellerAuctions.length > 0 && (
        <Box mb={6}>
          <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, mb: 3 }}>
            {t('auction.yourAuctions')}
          </Typography>
          <Grid container spacing={3}>
            {sellerAuctions.map(a => (
              <Grid component="div" key={a.id} size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }}>
                <AuctionCard auction={a} showStatus />
              </Grid>
            ))}
          </Grid>
          <Divider sx={{ mt: 6, mb: 4 }} />
        </Box>
      )}
      <Box>
        {isSeller && sellerAuctions.length > 0 && (
          <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, mb: 3 }}>
            {t('auction.otherAvailableAuctions')}
          </Typography>
        )}
        {auctions.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {t('auction.none')}
          </Typography>
        ) : (
          <Grid container spacing={3}>
            {auctions.map(a => (
              <Grid component="div" key={a.id} size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }}>
                <AuctionCard auction={a} />
              </Grid>
            ))}
          </Grid>
        )}
      </Box>
    </Container>
  );
}
