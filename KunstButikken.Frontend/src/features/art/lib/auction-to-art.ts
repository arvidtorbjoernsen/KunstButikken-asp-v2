import type { UiArt } from '@/features/art/types/art';
import type { UiAuction } from '@/features/auction/types/auction';

const UNTITLED = 'Untitled';

export function createAuctionArtMapper(allArt: UiArt[]) {
  const artMap = new Map(allArt.map(art => [art.id, art]));

  return (auction: UiAuction): UiArt => {
    const base = artMap.get(auction.artId);

    return {
      id: base?.id ?? auction.artId ?? auction.id,
      titleNb: base?.titleNb ?? auction.artTitleNb ?? auction.artTitleEn ?? UNTITLED,
      titleEn: base?.titleEn ?? auction.artTitleEn ?? auction.artTitleNb ?? UNTITLED,
      descriptionNb: base?.descriptionNb ?? auction.descriptionNb,
      descriptionEn: base?.descriptionEn ?? auction.descriptionEn,
      image: base?.image ?? auction.artImage,
      price: base?.price ?? auction.highestBid ?? auction.startingPrice ?? 0,
      sellerId: base?.sellerId ?? auction.sellerId,
      sellerDisplayName: base?.sellerDisplayName ?? auction.sellerDisplayName,
      artist: base?.artist ?? auction.artist ?? base?.sellerDisplayName ?? auction.sellerDisplayName ?? 'Unknown Artist',
      status: base?.status,
      isVerified: base?.isVerified ?? true,
      isFeatured: base?.isFeatured,
      createdAt: base?.createdAt,
    };
  };
}

