// Server component: Auction detail page with SSR
import Container from '@mui/material/Container';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid';
import type { UiAuction } from '@/features/auction/types/auction';
import type { UiArt } from '@/features/art/types/art';
import AuctionLiveClient from './live-client';
import ArtDetails from './ArtDetails';
import { createRequestScope } from '@/infrastructure/di/container';
import { GetAuctionById } from '@/application/useCases/GetAuctionById';
import { GetArtById } from '@/application/useCases/GetArtById';

function enrichAuctionWithArt(auction: UiAuction, art: UiArt | null): UiAuction {
  if (!art) return auction;
  return {
    ...auction,
    artImage: art.image ?? auction.artImage,
    artTitleEn: art.titleEn ?? auction.artTitleEn,
    artTitleNb: art.titleNb ?? auction.artTitleNb,
    artist: art.artist ?? auction.artist,
    sellerDisplayName: art.sellerDisplayName ?? auction.sellerDisplayName,
    descriptionNb: art.descriptionNb ?? auction.descriptionNb,
    descriptionEn: art.descriptionEn ?? auction.descriptionEn,
  };
}

export default async function AuctionDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const container = createRequestScope();
  const getAuctionById = container.resolve(GetAuctionById);
  const getArtById = container.resolve(GetArtById);
  const data = await getAuctionById.execute(id);
  if (!data) {
    return (
      <Container maxWidth='md' sx={{ py: 8 }}>
        <Typography>Not found</Typography>
      </Container>
    );
  }
  const art = await getArtById.execute(data.artId);
  const auction = enrichAuctionWithArt(data, art);

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
