import { parseResponse } from '@/shared/api';
import type { ApiArt, UiArt } from '@/features/art/types/art';

const gateway =
  process.env.NEXT_PUBLIC_API_GATEWAY ||
  process.env.NEXT_PUBLIC_API_BASE_URL ||
  process.env.NEXT_PUBLIC_API_ART ||
  'http://localhost:5000';

export async function getFeaturedServer(limit = 5): Promise<UiArt[]> {
  const url = `${gateway.replace(/\/$/, '')}/api/art/featured?limit=${limit}`;
  const res = await fetch(url, { next: { revalidate: 60 } });
  if (!res.ok) return [];
  const data = await parseResponse<ApiArt[]>(res);
  return (Array.isArray(data) ? data : []).map(mapApiToUi);
}

export async function getAllServer(): Promise<UiArt[]> {
  const url = `${gateway.replace(/\/$/, '')}/api/art`;
  const res = await fetch(url, { next: { revalidate: 60 } });
  if (!res.ok) return [];
  const data = await parseResponse<ApiArt[]>(res);
  return (Array.isArray(data) ? data : []).map(mapApiToUi);
}

export async function getByIdServer(id: string): Promise<UiArt | null> {
  const url = `${gateway.replace(/\/$/, '')}/api/art/${encodeURIComponent(id)}`;
  const res = await fetch(url, { next: { revalidate: 60 } });
  if (!res.ok) return null;
  const data = await parseResponse<ApiArt>(res);
  if (!data || !(data as any).id) return null;
  return mapApiToUi(data);
}

function mapApiToUi(a: ApiArt): UiArt {
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
    status: (a as any).status ?? undefined,
    isVerified: (a as any).isVerified ?? undefined,
    isFeatured: (a as any).isFeatured ?? undefined,
  };
}
