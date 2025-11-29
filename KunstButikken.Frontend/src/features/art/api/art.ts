import type { ApiArt, UiArt } from '@/features/art/types/art';

export function mapApiToUi(a: ApiArt): UiArt {
  return {
    id: String(a.id ?? ''),
    titleNb: a.titleNb ?? a.titleEn ?? 'Untitled',
    titleEn: a.titleEn ?? a.titleNb ?? 'Untitled',
    descriptionNb: a.descriptionNb ?? undefined,
    descriptionEn: a.descriptionEn ?? undefined,
    artist: a.artist ?? a.sellerDisplayName ?? a.sellerId ?? 'Unknown Artist',
    sellerId: a.sellerId ?? undefined,
    sellerDisplayName: a.sellerDisplayName ?? undefined,
    price: a.price ?? 0,
    image: a.imageUrl ?? '',
    status: a.status as UiArt['status'],
    isVerified: a.isVerified ?? undefined,
    isFeatured: a.isFeatured ?? undefined,
  };
}
