"use client";

import React from "react";

import Card from "@mui/material/Card";
import CardMedia from "@mui/material/CardMedia";
import CardContent from "@mui/material/CardContent";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import Chip from "@mui/material/Chip";
import Link from "next/link";
import type { UiArt } from "@/features/art/types/art";
import {useTranslations} from "@/features/i18n/components/TranslationProvider";

const statusMap: Record<number, { label: string; color: 'default' | 'success' | 'error' | 'warning' }> = {
  0: { label: 'Draft', color: 'default' },
  1: { label: 'Published', color: 'success' },
  2: { label: 'Sold', color: 'error' },
  3: { label: 'Rejected', color: 'warning' },
};

const verifiedLabel = 'Verified';
const featuredLabel = 'Featured';

export default function ArtCard({ art, showStatus = false }: { art: UiArt; showStatus?: boolean }) {
  const { t, locale } = useTranslations();

  const hasImage = !!art.image;

  // Use language-specific title and description based on current locale
  const title = locale === 'nb' ? art.titleNb : art.titleEn;
  const description = locale === 'nb' ? art.descriptionNb : art.descriptionEn;

  const status = typeof art.status === 'number' ? statusMap[art.status] : null;

  // Helper function for currency formatting
  const formatCurrencyNOK = (n?: number) => {
    if (n == null) return '—';
    try {
      return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'NOK' }).format(n);
    } catch {
      return `Kr ${n}`;
    }
  };

  return (
    <Link href={`/art/${art.id}`} style={{ textDecoration: "none" }}>
      <Card elevation={1} sx={{ borderRadius: 2, cursor: "pointer" }}>
        <CardMedia
          component="img"
          image={hasImage ? (art.image || "/placeholder-600x400.svg") : "/placeholder-600x400.svg"}
          alt={title}
          title={title}
          onError={(e: React.SyntheticEvent<HTMLImageElement>) => { try { e.currentTarget.src = "/placeholder-600x400.svg"; } catch {} }}
          // increased xs height so cards are taller on small/phone screens
          sx={{ height: { xs: 260, sm: 500, md: 400, lg: 460, xl: 460 }, objectFit: "cover" }}
        />
        <CardContent>
          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
              <Typography
                variant="subtitle1"
                component="h3"
                sx={{ fontWeight: 600, fontSize: { xs: '0.95rem', sm: '1rem', md: '1.05rem', lg: '1.1rem' }, flex: 1 }}
              >
                {title}
              </Typography>
              {showStatus && (
                <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', ml: 1 }}>
                  {status && (
                    <Chip
                      label={status.label}
                      color={status.color}
                      size="small"
                      sx={{ height: 20, fontSize: '0.7rem' }}
                    />
                  )}
                  {art.isVerified && (
                    <Chip
                      label={verifiedLabel}
                      color="primary"
                      size="small"
                      sx={{ height: 20, fontSize: '0.7rem' }}
                    />
                  )}
                  {art.isFeatured && (
                    <Chip
                      label={featuredLabel}
                      color="secondary"
                      size="small"
                      sx={{ height: 20, fontSize: '0.7rem' }}
                    />
                  )}
                </Box>
              )}
            </Box>

            {/* Show short description from server (if provided) */}
            {description && (
              <Typography
                variant="body2"
                color="text.secondary"
                sx={{
                  mt: 0.5,
                  display: '-webkit-box',
                  WebkitLineClamp: 3,
                  WebkitBoxOrient: 'vertical',
                  overflow: 'hidden',
                }}
              >
                {description}
              </Typography>
            )}

            {/* Painting artist */}
            <Typography variant="caption" color="text.secondary" sx={{ display: "block", fontSize: { xs: '0.7rem', sm: '0.8rem', md: '0.85rem' }, mt: 0.5 }}>
              {`${t("art.artist")}: ${art.artist ?? t("art.unknownArtist")}`}
            </Typography>
            {/* Seller display name (do not show user id) */}
            {art.sellerDisplayName && (
              <Typography variant="caption" color="text.secondary" sx={{ display: "block", fontSize: { xs: '0.7rem', sm: '0.8rem', md: '0.85rem' } }}>
                {`${t("art.seller")}: ${art.sellerDisplayName}`}
              </Typography>
            )}
            <Typography
              variant="body2"
              sx={{ mt: 1, fontWeight: 500, fontSize: { xs: '0.9rem', sm: '1rem', md: '1.05rem' } }}
            >{`${t("art.price")}: ${formatCurrencyNOK(art.price)}`}</Typography>

            {/* Removed visible seed; the backend now seeds Title/Description and frontend displays them */}
          </Box>
        </CardContent>
      </Card>
    </Link>
  );
}
