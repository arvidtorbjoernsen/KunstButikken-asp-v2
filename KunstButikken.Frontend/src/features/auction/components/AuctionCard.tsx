'use client';

import React from 'react';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardMedia from '@mui/material/CardMedia';
import Typography from '@mui/material/Typography';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import CardActionArea from '@mui/material/CardActionArea';
import NextLink from 'next/link';
import { useRouter } from 'next/navigation';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import type { UiAuction } from '../types/auction';
import Chip from '@mui/material/Chip';

function formatCurrency(n?: number) {
  if (n == null) return '—';
  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: 'NOK' }).format(n); // Changed to NOK
  } catch {
    return `Kr ${n}`; // Changed to Kr
  }
}

function useCountdown(target: Date) {
  const [now, setNow] = React.useState<number>(() => Date.now());
  React.useEffect(() => {
    const id = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(id);
  }, []);
  const ms = Math.max(0, target.getTime() - now);
  const totalSeconds = Math.floor(ms / 1000);
  const d = Math.floor(totalSeconds / 86400);
  const h = Math.floor((totalSeconds % 86400) / 3600);
  const m = Math.floor((totalSeconds % 3600) / 60);
  const s = totalSeconds % 60;
  if (d > 0) return `${d}d ${h}h ${m}m`;
  if (h > 0) return `${h}h ${m}m ${s}s`;
  return `${m}m ${s}s`;
}

export default function AuctionCard({
  auction,
  showStatus = false,
}: {
  auction: UiAuction;
  showStatus?: boolean;
}) {
  const { t, locale } = useTranslations(); // Destructure locale here
  const router = useRouter();

  const title = `${t('nav.auctions')} — ${auction.id.substring(0, 8)}`;
  const artTitle =
    locale === 'nb' // Use locale directly
      ? (auction.artTitleNb ?? auction.artTitleEn)
      : (auction.artTitleEn ?? auction.artTitleNb);
  const countdown = useCountdown(auction.endsAt);

  const goToAuction = () => router.push(`/art/auctions/${auction.id}`);

  return (
    <Card elevation={1} sx={{ borderRadius: 2 }}>
      <CardActionArea onClick={goToAuction} sx={{ borderRadius: 2 }}>
        <CardMedia
          component="img"
          image={auction.artImage || '/placeholder-600x400.svg'}
          alt={artTitle || title}
          title={artTitle || title}
          onError={(e: React.SyntheticEvent<HTMLImageElement>) => {
            try {
              e.currentTarget.src = '/placeholder-600x400.svg';
            } catch {}
          }}
          sx={{ height: { xs: 260, sm: 500, md: 400, lg: 460, xl: 460 }, objectFit: 'cover' }}
        />
        <CardContent>
          <Box>
            <Box
              sx={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'flex-start',
                mb: 1,
              }}
            >
              <Typography
                variant="subtitle1"
                component="h3"
                sx={{
                  fontWeight: 600,
                  fontSize: { xs: '0.95rem', sm: '1rem', md: '1.05rem', lg: '1.1rem' },
                  flex: 1,
                }}
              >
                {artTitle || title}
              </Typography>
              {showStatus && (
                <Box
                  sx={{
                    display: 'flex',
                    gap: 0.5,
                    flexWrap: 'wrap',
                    ml: 1,
                    justifyContent: 'flex-end',
                  }}
                >
                  {/* Status chip */}
                  <Chip
                    label={t(`auction.${auction.status.toLowerCase()}`)}
                    color={
                      auction.isOpen
                        ? 'success'
                        : auction.isClosed
                          ? auction.reserveMet
                            ? 'default'
                            : 'error'
                          : 'warning'
                    }
                    size="small"
                    sx={{ height: 20, fontSize: '0.7rem' }}
                  />

                  {/* Reserve chip */}
                  <Chip
                    label={
                      auction.reservePrice == null
                        ? t('auction.noReserve')
                        : auction.reserveMet
                          ? t('auction.reserveMet')
                          : t('auction.reserveNotMet')
                    }
                    color={
                      auction.reservePrice == null
                        ? 'default'
                        : auction.reserveMet
                          ? 'primary'
                          : 'error'
                    }
                    variant={auction.reserveMet ? 'filled' : 'outlined'}
                    size="small"
                    sx={{ height: 20, fontSize: '0.7rem' }}
                  />

                  {/* Bids count chip */}
                  <Chip
                    label={`${t('auction.bids')}: ${auction.bidsCount}`}
                    color="info"
                    size="small"
                    sx={{ height: 20, fontSize: '0.7rem' }}
                  />

                  {/* Highest bid chip (only when there are bids) */}
                  {auction.highestBid != null && (
                    <Chip
                      label={`${t('auction.top')}: ${formatCurrency(auction.highestBid)}`}
                      color="secondary"
                      size="small"
                      sx={{ height: 20, fontSize: '0.7rem' }}
                    />
                  )}
                </Box>
              )}
            </Box>
            {auction.artist && (
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{
                  display: 'block',
                  fontSize: { xs: '0.7rem', sm: '0.8rem', md: '0.85rem' },
                  mt: 0.5,
                }}
              >
                {`${t('art.artist')}: ${auction.artist}`}
              </Typography>
            )}
            {auction.sellerDisplayName && (
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ display: 'block', fontSize: { xs: '0.7rem', sm: '0.8rem', md: '0.85rem' } }}
              >
                {`${t('art.seller')}: ${auction.sellerDisplayName}`}
              </Typography>
            )}
            <Typography
              variant="body2"
              sx={{ mt: 1, fontWeight: 500, fontSize: { xs: '0.9rem', sm: '1rem', md: '1.05rem' } }}
            >
              {`${t('art.price')}: ${formatCurrency(auction.startingPrice)}`}
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
              {`${t('auction.highestBid')}: ${formatCurrency(auction.highestBid)}`}{' '}
              • {`${t('auction.bids')}: ${auction.bidsCount}`}
            </Typography>
            <Typography variant="body2">
              {t('auction.reserve')}: {auction.reserveMet ? t('auction.reserveMetStatus') : t('auction.reserveNotMetStatus')}
            </Typography>
            <Typography variant="body2">
              {t('auction.endsIn')}: {countdown}
            </Typography>
          </Box>
          <Box sx={{ mt: 1 }}>
            <Button
              component={NextLink}
              href={`/art/${auction.artId}`}
              onClick={e => e.stopPropagation()}
              size="small"
              variant="outlined"
            >
              {t('auction.viewArtwork')}
            </Button>
          </Box>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
