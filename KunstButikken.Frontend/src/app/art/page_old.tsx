"use client";

import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Grid from "@mui/material/Grid";
import Typography from "@mui/material/Typography";
import { useEffect, useState } from "react";

import ArtCard from "@/features/art/components/ArtCard";
import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import { getAllClient } from '@/features/art/api/art-client';
import type { UiArt } from "@/features/art/types/art";

 export default function ArtForSalePage() {
   const { t } = useTranslations();

  const [allArt, setAllArt] = useState<UiArt[]>([]);
   const [loading, setLoading] = useState(true);

   useEffect(() => {
     let mounted = true;
    (async () => {
      try {
        const all = await getAllClient();
        if (!mounted) return;
        setAllArt(all);
      } catch (err) {
        console.warn('Failed to fetch art client-side:', err);
        if (!mounted) return;
        setAllArt([]);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
     return () => { mounted = false; };
   }, []);

  // All art will be displayed since featured section is removed

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
           {t("artPage.forSale")}
         </Typography>
         <Typography variant="body2" color="text.secondary">
           {t("nav.art")}
         </Typography>
       </Box>

      {allArt.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          {t("artPage.noArt") ?? "No art found."}
        </Typography>
      ) : (
        <Grid container spacing={3}>
          {allArt.map((art) => (
            <Grid size={{ xs: 12, sm: 12, md: 6, lg: 4, xl: 3 }} key={art.id}>
              <ArtCard art={art} />
            </Grid>
          ))}
        </Grid>
      )}
     </Container>
   );
 }
