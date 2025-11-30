'use client';

import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import { useEffect, useMemo, useState } from 'react';
import { useKeycloak } from '@/features/auth/lib/keycloak';
import { useTranslations } from '@/features/i18n/components/TranslationProvider';
import AuctionsListClient from './AuctionsListClient';
import type { UiAuction } from '@/features/auction/types/auction';
import { useContainer } from '@/presentation/providers/DiProvider';
import { GetAuctions } from '@/application/useCases/GetAuctions';
import { getByIdClient as getArtByIdClient } from '@/features/art/api/art-client';

export default function AuctionsPageClient({ initialAuctions }: { initialAuctions: UiAuction[] }) {
  const { t } = useTranslations();
  const { keycloak, isSeller, isBuyer } = useKeycloak();
  const container = useContainer();
  const getAuctionsUseCase = useMemo(() => container.resolve(GetAuctions), [container]);
  const [mine, setMine] = useState<UiAuction[]>([]);
  const [others, setOthers] = useState<UiAuction[]>(initialAuctions);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    let mounted = true;
    (async () => {
      setLoading(true);
      try {
        const all = await getAuctionsUseCase.execute('Open');
        if (!mounted) return;
        const enriched = await Promise.all(
          all.map(async auction => {
            try {
              const art = await getArtByIdClient(auction.artId);
              if (art) {
                return {
                  ...auction,
                  artImage: art.image || auction.artImage,
                  artTitleNb: art.titleNb,
                  artTitleEn: art.titleEn,
                  artist: art.artist,
                  sellerDisplayName: auction.sellerDisplayName || art.sellerDisplayName,
                } satisfies UiAuction;
              }
            } catch {
              // ignore enrichment errors
            }
            return auction;
          }),
        );
        const sub = keycloak?.tokenParsed?.['sub'];
        if (isSeller && sub) {
          setMine(enriched.filter(a => a.sellerId === sub));
          setOthers(enriched.filter(a => a.sellerId !== sub));
        } else {
          setMine([]);
          setOthers(enriched);
        }
      } catch {
        if (!mounted) return;
        setOthers(initialAuctions);
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, [getAuctionsUseCase, initialAuctions, isSeller, keycloak]);

  if (loading && others.length === 0) {
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
      preloaded={!loading && !!initialAuctions.length}
    />
  );
}
