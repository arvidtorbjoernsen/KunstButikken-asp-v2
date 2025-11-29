"use client";

import ArtCard from "@/features/art/components/ArtCard";
import { useTranslations } from "@/features/i18n/components/TranslationProvider";
import type { UiArt } from "@/features/art/types/art";
import Box from "@mui/material/Box";
import Container from "@mui/material/Container";
import Grid from "@mui/material/Grid";
import Typography from "@mui/material/Typography";

interface HomePageClientProps {
  initialFeatured: UiArt[];
  initialAll?: UiArt[];
}

/**
 * Client-side home page component
 * Receives server-rendered data and provides interactivity
 * Similar to Angular components that receive data from resolvers
 */
export default function HomePageClient({ initialFeatured, initialAll }: HomePageClientProps) {
  const { t } = useTranslations();


  return (
    <Container maxWidth="xl" sx={{ py: 8 }}>
      <Box mb={6}>
        <Typography variant="h4" component="h1" gutterBottom>
          {t("home.title")}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t("home.subtitle")}
        </Typography>
      </Box>

      {initialFeatured && initialFeatured.length > 0 && (
        <Box mb={4}>
          <Typography variant="h6" gutterBottom>
            {t("home.featured")}
          </Typography>

          <Grid container spacing={3}>
            {initialFeatured.map((art) => (
              <Grid size={{ xs: 12, sm: 12, md: 6, lg: 4, xl: 3 }} key={art.id}>
                <ArtCard art={art} />
              </Grid>
            ))}
          </Grid>
        </Box>
      )}

      {/* All Art Section (server-rendered) */}
      <Box mb={4} mt={6}>
        <Typography variant="h6" gutterBottom>
          {t('home.allArt')}
        </Typography>

        {initialAll && initialAll.length > 0 ? (
          <Grid container spacing={3}>
            {initialAll.map((art) => (
              <Grid size={{ xs: 12, sm: 6, md: 6, lg: 4, xl: 3 }} key={art.id}>
                <ArtCard art={art} />
              </Grid>
            ))}
          </Grid>
        ) : (
          <Box sx={{ textAlign: "center", py: 4 }}>
            <Typography variant="body2" color="text.secondary">
              {t('home.noArtwork')}
            </Typography>
          </Box>
        )}
      </Box>

      {(!initialFeatured || initialFeatured.length === 0) && (
        <Box sx={{ textAlign: 'center', py: 4 }}>
          <Typography variant="body2" color="text.secondary">
            {t('home.noArtwork')}
          </Typography>
        </Box>
      )}
    </Container>
  );
}
