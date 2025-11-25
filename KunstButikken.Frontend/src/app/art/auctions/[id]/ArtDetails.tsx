'use client';

import React from 'react';
import { CardMedia, Divider, Grid, Paper, Typography, Box } from '@mui/material'; // Import Box
import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import type { UiAuction } from '@/features/auction/types/auction'; // Import UiAuction type

export default function ArtDetails({ art }: { art: UiAuction }) {
  const { locale, t } = useTranslations();

  if (!art) {
    return null;
  }

  console.log('[ArtDetails] Image src:', art.artImage || '/placeholder.png');

  const description = locale === 'nb' ? art.descriptionNb : art.descriptionEn; // Get localized description
  const byText = t('auction.by');
  const capitalizedByText = byText.charAt(0).toUpperCase() + byText.slice(1);

  return (
    <Paper sx={{ p: 2, height: '100%' }}>
      <Grid container spacing={2} direction='row' sx={{ height: '100%' }}>
        <Grid size={{ xs: 12, md: 6 }}> {/* Text content on the left */}
          <Typography variant='h4' component='h1' gutterBottom>
            {locale === 'nb' ? art.artTitleNb : art.artTitleEn}{' '}
          </Typography>
          <Typography variant='h6' color='text.secondary' gutterBottom>
            {capitalizedByText} {art.artist}
          </Typography>

          {description && ( // Display description if available
            <Box sx={{ mt: 2, mb: 2 }}>
              <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-wrap' }}>
                {description}
              </Typography>
            </Box>
          )}

          <Divider sx={{ my: 2 }} /> {/* This divider will now be below the description */}

          <Typography variant='body2' color='text.secondary'>
            {t('art.seller')}: {art.sellerDisplayName}
          </Typography>
        </Grid>
        <Grid size={{ xs: 12, md: 6 }}> {/* Image on the right */}
          <CardMedia
            component='img'
            image={art.artImage || '/placeholder.png'}
            alt={art.artTitleEn || t('auction.untitled')}
            sx={{ objectFit: 'contain', height: '100%', width: '100%' }}
          />
        </Grid>
      </Grid>
    </Paper>
  );
}
