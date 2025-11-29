"use client";

import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import type { UiArt } from '@/features/art/types/art';
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CardMedia from "@mui/material/CardMedia";
import Container from "@mui/material/Container";
import Typography from "@mui/material/Typography";
import Chip from "@mui/material/Chip";
import Divider from "@mui/material/Divider";
import Grid from "@mui/material/Grid";

interface ArtDetailClientProps {
  art: UiArt | null;
}

const statusMap: Record<number, { label: string; color: 'default' | 'success' | 'error' | 'warning' }> = {
  0: { label: 'Draft', color: 'default' },
  1: { label: 'Published', color: 'success' },
  2: { label: 'Sold', color: 'error' },
  3: { label: 'Rejected', color: 'warning' },
};

export default function ArtDetailClient({ art }: ArtDetailClientProps) {
  const { locale, t } = useTranslations();

  if (!art) {
    return (
      <Container sx={{ py: 6 }}>
        <Typography variant="body2" color="text.secondary">
          Art not found.
        </Typography>
      </Container>
    );
  }

  const title = locale === 'nb' ? art.titleNb : art.titleEn;
  const description = locale === 'nb' ? art.descriptionNb : art.descriptionEn;
  const status = art.status != null ? statusMap[art.status] : null;

  return (
    <Container maxWidth="lg" sx={{ py: 6 }}>
      <Grid container spacing={4}>
        {/* Image Section */}
        <Grid size={{ xs: 12, md: 7 }}>
          {art.image ? (
            <Card sx={{ borderRadius: 2, overflow: 'hidden' }}>
              <CardMedia
                component="img"
                image={art.image}
                alt={title}
                sx={{
                  width: '100%',
                  height: { xs: 300, sm: 400, md: 500 },
                  objectFit: 'cover'
                }}
              />
            </Card>
          ) : (
            <Card sx={{ borderRadius: 2, overflow: 'hidden', bgcolor: 'grey.200', height: { xs: 300, sm: 400, md: 500 }, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Typography variant="body2" color="text.secondary">
                No image available
              </Typography>
            </Card>
          )}
        </Grid>

        {/* Details Section */}
        <Grid size={{ xs: 12, md: 5 }}>
          <Card sx={{ borderRadius: 2 }}>
            <CardContent>
              {/* Title and Status */}
              <Box sx={{ mb: 2 }}>
                <Typography variant="h4" component="h1" gutterBottom sx={{ fontWeight: 600 }}>
                  {title}
                </Typography>
                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                  {status && (
                    <Chip
                      label={status.label}
                      color={status.color}
                      size="small"
                    />
                  )}
                  {art.isVerified && (
                    <Chip
                      label="Verified"
                      color="primary"
                      size="small"
                      variant="outlined"
                    />
                  )}
                  {art.isFeatured && (
                    <Chip
                      label="Featured"
                      color="secondary"
                      size="small"
                      variant="outlined"
                    />
                  )}
                </Box>
              </Box>

              <Divider sx={{ my: 2 }} />

              {/* Price */}
              <Box sx={{ mb: 3 }}>
                <Typography variant="h5" color="primary" sx={{ fontWeight: 600 }}>
                  {new Intl.NumberFormat(undefined, {
                    style: "currency",
                    currency: "NOK" // Changed to NOK
                  }).format(Number(art.price))}
                </Typography>
              </Box>

              {/* Artist and Seller Info */}
              <Box sx={{ mb: 3 }}>
                {art.artist && (
                  <Box sx={{ mb: 1 }}>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      {t('art.artist') || 'Artist'}
                    </Typography>
                    <Typography variant="body1" sx={{ fontWeight: 500 }}>
                      {art.artist}
                    </Typography>
                  </Box>
                )}
                {art.sellerDisplayName && (
                  <Box sx={{ mb: 1 }}>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      {t('art.seller') || 'Seller'}
                    </Typography>
                    <Typography variant="body1" sx={{ fontWeight: 500 }}>
                      {art.sellerDisplayName}
                    </Typography>
                  </Box>
                )}
                {art.createdAt && (
                  <Box>
                    <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                      Listed on
                    </Typography>
                    <Typography variant="body2">
                      {new Date(art.createdAt).toLocaleDateString(undefined, {
                        year: 'numeric',
                        month: 'long',
                        day: 'numeric'
                      })}
                    </Typography>
                  </Box>
                )}
              </Box>

              {/* Description */}
              {description && (
                <>
                  <Divider sx={{ my: 2 }} />
                  <Box>
                    <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 600 }}>
                      Description
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-wrap' }}>
                      {description}
                    </Typography>
                  </Box>
                </>
              )}

              {/* Additional Info */}
              <Divider sx={{ my: 2 }} />
              <Box>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
                  Art ID
                </Typography>
                <Typography variant="body2" sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                  {art.id}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Container>
  );
}
