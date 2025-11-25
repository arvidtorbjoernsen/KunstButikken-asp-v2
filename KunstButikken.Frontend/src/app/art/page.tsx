'use client';

import Box from '@mui/material/Box';
import Container from '@mui/material/Container';
import Grid from '@mui/material/Grid';
import Typography from '@mui/material/Typography';
import Divider from '@mui/material/Divider';
import { useEffect, useState } from 'react';

import ArtCard from '@/features/art/components/ArtCard';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import { useKeycloak } from '@/features/auth/lib/keycloak';
import { getAllClient } from '@/features/art/api/art-client';
import type { UiArt } from '@/features/art/types/art';

export default function ArtForSalePage() {
  const { t } = useTranslations();
  const { authenticated, isSeller, isBuyer, keycloak } = useKeycloak();

  const [allArt, setAllArt] = useState<UiArt[]>([]);
  const [myArt, setMyArt] = useState<UiArt[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const all = await getAllClient();
        if (!mounted) return;

        const userId = keycloak?.tokenParsed?.['sub'];

        console.log('[ArtPage] Debug info:');
        console.log('  - Total art items:', all.length);
        console.log('  - User ID (sub):', userId);
        console.log('  - isSeller:', isSeller);
        console.log('  - isBuyer:', isBuyer);
        console.log('  - authenticated:', authenticated);
        console.log('  - Sample art item:', all[0]);

        // Filter based on role
        let filteredArt = all;
        let sellerArt: UiArt[] = [];

        if (isSeller && userId) {
          // Sellers see their own art separately
          sellerArt = all.filter(art => {
            const match = art.sellerId === userId;
            if (match) console.log('  - Found MY art:', art.titleEn, 'sellerId:', art.sellerId);
            return match;
          });
          filteredArt = all.filter(art => art.sellerId !== userId);
          console.log('  - My art count:', sellerArt.length);
          console.log('  - Other art count:', filteredArt.length);
        } else if (isBuyer || authenticated) {
          // Buyers only see verified art
          filteredArt = all.filter(art => art.isVerified === true);
          console.log('  - Buyer sees verified art count:', filteredArt.length);
        }
        // Non-authenticated users see all (or we could restrict further)

        setMyArt(sellerArt);
        setAllArt(filteredArt);
      } catch (err) {
        console.warn('Failed to fetch art client-side:', err);
        if (!mounted) return;
        setAllArt([]);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, [authenticated, keycloak, isSeller, isBuyer]);

  if (loading) {
    return (
      <Container maxWidth="xl" sx={{ py: 8 }}>
        <Typography>{t('home.loading') ?? 'Loading artworks...'}</Typography>
      </Container>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
      <Box mb={6}>
        <Typography variant="h4" component="h1" gutterBottom>
          {t('artPage.forSale')}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {isSeller
            ? 'Your art and all available artworks'
            : isBuyer
              ? 'Verified artworks available for purchase'
              : t('nav.art')}
        </Typography>
      </Box>

      {/* Seller's Own Art Section */}
      {isSeller && myArt.length > 0 && (
        <Box mb={6}>
          <Typography variant="h6" gutterBottom sx={{ fontWeight: 600 }}>
            My Artworks
          </Typography>
          <Grid container spacing={3}>
            {myArt.map(art => (
              <Grid size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }} key={art.id}>
                <ArtCard art={art} showStatus />
              </Grid>
            ))}
          </Grid>
          <Divider sx={{ mt: 6, mb: 4 }} />
        </Box>
      )}

      {/* All Other Art */}
      <Box>
        {isSeller && myArt.length > 0 && (
          <Typography variant="h6" gutterBottom sx={{ fontWeight: 600, mb: 3 }}>
            All Other Artworks
          </Typography>
        )}
        {allArt.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {t('artPage.noArt') ?? 'No art found.'}
          </Typography>
        ) : (
          <Grid container spacing={3}>
            {allArt.map(art => (
              <Grid size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }} key={art.id}>
                <ArtCard art={art} />
              </Grid>
            ))}
          </Grid>
        )}
      </Box>
    </Container>
  );
}
