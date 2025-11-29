import 'reflect-metadata';
import { inject, injectable } from 'tsyringe';
import type { UiArt, ApiArt } from '@/features/art/types/art';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { ApiClient } from '@/infrastructure/http/types';
import { buildServiceUrl } from '@/shared/config';
import { parseResponse } from '@/shared/api';
import type { IArtRepository } from '@/application/interfaces/IArtRepository';

@injectable()
export class ArtRepository implements IArtRepository {
  constructor(@inject(DI_TOKENS.ApiClient) private readonly apiClient: ApiClient) {}

  async getFeatured(limit = 5): Promise<UiArt[]> {
    const url = buildServiceUrl('ART', `/featured?limit=${limit}`);
    return this.fetchCollection(url);
  }

  async getAll(): Promise<UiArt[]> {
    const url = buildServiceUrl('ART', '/');
    return this.fetchCollection(url);
  }

  async getById(id: string): Promise<UiArt | null> {
    const url = buildServiceUrl('ART', `/${encodeURIComponent(id)}`);
    const res = await fetch(url, { next: { revalidate: 60 } });
    if (!res.ok) {
      return null;
    }
    const data = await parseResponse<ApiArt>(res);
    return data ? this.mapApiToUi(data) : null;
  }

  private async fetchCollection(url: string): Promise<UiArt[]> {
    const res = await fetch(url, { next: { revalidate: 60 } });
    if (!res.ok) {
      return [];
    }
    const data = await parseResponse<ApiArt[]>(res);
    return Array.isArray(data) ? data.map(this.mapApiToUi) : [];
  }

  private mapApiToUi = (a: ApiArt): UiArt => ({
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
  });
}
