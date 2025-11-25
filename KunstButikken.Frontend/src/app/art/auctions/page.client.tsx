'use client';

import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { useKeycloak } from '@/features/auth/lib/keycloak';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import { getAuctionsClient } from '@/features/auction/api/auction-client';
import AuctionsListClient from './AuctionsListClient';
import type { UiAuction } from '@/features/auction/types/auction';
import { getByIdClient as getArtByIdClient } from '@/features/art/api/art-client';

export default function AuctionsPageClient() {
  const { t } = useTranslations();
  const { keycloak, isSeller, isBuyer, authenticated } = useKeycloak();
  const [mine, setMine] = useState<UiAuction[]>([]);
  const [others, setOthers] = useState<UiAuction[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const all = await getAuctionsClient('Open');
        if (!mounted) return;
        const sub = keycloak?.tokenParsed?.['sub'];

        // Enrich auctions with art image/title/artist
        const enriched = await Promise.all(
          all.map(async a => {
            try {
              const art = await getArtByIdClient(a.artId);
              if (art) {
                return {
                  ...a,
                  artImage: art.image || a.artImage,
                  artTitleNb: art.titleNb,
                  artTitleEn: art.titleEn,
                  artist: art.artist,
                  sellerDisplayName: a.sellerDisplayName || art.sellerDisplayName,
                } as UiAuction;
              }
            } catch (e) {
              // ignore
            }
            return a;
          }),
        );

        console.log(
          '[AuctionsPage] Debug: total',
          enriched.length,
          'sub',
          sub,
          'isSeller',
          isSeller,
        );

        if (isSeller && sub) {
          const my = enriched.filter(a => a.sellerId === sub);
          const rest = enriched.filter(a => a.sellerId !== sub);
          setMine(my);
          setOthers(rest);
        } else {
          setMine([]);
          setOthers(enriched);
        }
      } catch (e) {
        console.warn('Failed to load auctions:', e);
        if (!mounted) return;
        setMine([]);
        setOthers([]);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, [keycloak, authenticated, isSeller, isBuyer]);

  if (loading) {
    return (
      <Container maxWidth="xl" sx={{ py: 8 }}>
        <Typography>{t('home.loading')}</Typography>
      </Container>
    );
  }

  return (
    <AuctionsListClient
      auctions={others}
      sellerAuctions={mine}
      isSeller={isSeller}
      isBuyer={isBuyer}
    />
  );
}
