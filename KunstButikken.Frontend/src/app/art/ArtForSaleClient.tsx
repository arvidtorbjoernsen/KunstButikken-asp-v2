"use client";
import Container from '@mui/material/Container';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid';
import Divider from '@mui/material/Divider';
import ArtCard from '@/features/art/components/ArtCard';
import type { UiArt } from '@/features/art/types/art';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';

export default function ArtForSaleClient({ mine, others, isSeller, isBuyer }: { mine: UiArt[]; others: UiArt[]; isSeller?: boolean; isBuyer?: boolean }) {
  const { t } = useTranslations();

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

      {isSeller && mine.length > 0 && (
        <Box mb={6}>
          <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, mb: 3 }}>
            {t('artPage.yourArt')}
          </Typography>
          <Grid container spacing={3}>
            {mine.map(a => (
              <Grid key={a.id} size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }}>
                <ArtCard art={a} showStatus />
              </Grid>
            ))}
          </Grid>
          <Divider sx={{ mt: 6, mb: 4 }} />
        </Box>
      )}

      <Box>
        {isSeller && mine.length > 0 && (
          <Typography variant="h5" gutterBottom sx={{ fontWeight: 600, mb: 3 }}>
            {t('artPage.otherArt')}
          </Typography>
        )}
        {others.length === 0 ? (
          <Typography variant="body2" color="text.secondary">{t('artPage.noArt') ?? 'No art found.'}</Typography>
        ) : (
          <Grid container spacing={3}>
            {others.map(a => (
              <Grid key={a.id} size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }}>
                <ArtCard art={a} />
              </Grid>
            ))}
          </Grid>
        )}
      </Box>
    </Container>
  );
}
