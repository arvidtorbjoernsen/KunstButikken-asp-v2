// Server component: Auction detail page with SSR
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid';
import { getAuctionByIdServer } from '@/features/auction/api/auction-server';
import type { ApiAuction, UiAuction } from '@/features/auction/types/auction';
import AuctionLiveClient from './live-client';
import ArtDetails from './ArtDetails';
import { getByIdServer as getArtByIdServer } from '@/features/art/api/art';

function mapApiToUi(a: ApiAuction, art: any): UiAuction {
  const startsAt = new Date(a.startsAt);
  const endsAt = new Date(a.endsAt);
  const bids = Array.isArray(a.bids) ? a.bids : [];
  const highestBid = bids.length ? Math.max(...bids.map(b => b.amount)) : undefined;
  const now = Date.now();
  const timeLeftMs = endsAt.getTime() - now;
  const hasStarted = startsAt.getTime() <= now;
  const hasEnded = timeLeftMs <= 0;
  const isOpen = a.status === 'Open' && hasStarted && !hasEnded;
  const isClosed = a.status === 'Closed' || hasEnded || !hasStarted;
  const reservePrice = a.reservePrice ?? undefined;
  const reserveMet = reservePrice == null || (highestBid != null && highestBid >= reservePrice);

  return {
    id: String(a.id),
    artId: String(a.artId),
    sellerId: String(a.sellerId),
    startsAt,
    endsAt,
    startingPrice: a.startingPrice ?? 0,
    reservePrice,
    status: a.status,
    bidsCount: bids.length,
    highestBid,
    isOpen,
    isClosed,
    reserveMet,
    timeLeftMs,
    artImage: art?.image,
    artTitleEn: art?.titleEn,
    artTitleNb: art?.titleNb,
    artist: art?.artist,
    sellerDisplayName: art?.sellerDisplayName,
    descriptionNb: art?.descriptionNb, // Added descriptionNb
    descriptionEn: art?.descriptionEn, // Added descriptionEn
  };
}

export default async function AuctionDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const data = await getAuctionByIdServer(id);
  if (!data) {
    return (
      <Container maxWidth='md' sx={{ py: 8 }}>
        <Typography>Not found</Typography>
      </Container>
    );
  }
  const art = await getArtByIdServer(data.artId);
  const auction = mapApiToUi(data, art);

  return (
    <Container maxWidth='xl' sx={{ py: 4 }}>
      <Grid container spacing={4}>
        <Grid size={{ xs: 12, md: 7 }}>
          <ArtDetails art={auction} />
        </Grid>
        <Grid size={{ xs: 12, md: 5 }}>
          <AuctionLiveClient auctionId={auction.id} initial={auction} />
        </Grid>
      </Grid>
    </Container>
  );
}
