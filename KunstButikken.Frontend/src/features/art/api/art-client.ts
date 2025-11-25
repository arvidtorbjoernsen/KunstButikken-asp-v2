import type { UiArt } from '@/features/art/types/art';
import { apiFetch } from '@/shared/api/api';

/**
 * Client-side helpers for fetching art via the AuthGateway.
 * These use `apiFetch` which attaches the Keycloak token when available.
 */
export async function getFeaturedClient(limit = 5): Promise<UiArt[]> {
  const data = await apiFetch<any[]>('ART', `/featured?limit=${limit}`);
  return (Array.isArray(data) ? data : []).map(mapApiToUi);
}

export async function getAllClient(searchQuery?: string): Promise<UiArt[]> {
  let url = `/`;
  if (searchQuery) {
    url += `?q=${encodeURIComponent(searchQuery)}`;
  }
  const data = await apiFetch<any[]>('ART', url);
  return (Array.isArray(data) ? data : []).map(mapApiToUi);
}

export async function getByIdClient(id: string): Promise<UiArt | null> {
  if (!id) return null;
  const data = await apiFetch<any>('ART', `/${encodeURIComponent(id)}`);
  if (!data || !data.id) return null;
  return mapApiToUi(data);
}

function mapApiToUi(a: any): UiArt {
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
    status: typeof a.status === 'number' ? a.status : undefined,
    isVerified: !!a.isVerified,
    isFeatured: !!a.isFeatured,
  } as UiArt;
}
