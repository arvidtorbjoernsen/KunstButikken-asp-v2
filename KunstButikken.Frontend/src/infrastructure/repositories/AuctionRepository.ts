import 'reflect-metadata';
import { inject, injectable } from 'tsyringe';
import type { IAuctionRepository, UpdateAuctionPayload } from '@/application/interfaces/IAuctionRepository';
import type { UiAuction } from '@/features/auction/types/auction';
import type { ApiAuction } from '@/features/auction/types/auction';
import { DI_TOKENS } from '@/infrastructure/di/tokens';
import type { ApiClient } from '@/infrastructure/http/types';
import { mapApiToUi } from '@/features/auction/api/auction-client';
import { buildServiceUrl } from '@/shared/config';
import { parseResponse } from '@/shared/api';

@injectable()
export class AuctionRepository implements IAuctionRepository {
  constructor(@inject(DI_TOKENS.ApiClient) private readonly apiClient: ApiClient) {}

  async getAuctions(status?: 'Open' | 'Closed' | 'Draft'): Promise<UiAuction[]> {
    const baseUrl = buildServiceUrl('AUCTION', '/');
    const url = status ? `${baseUrl}?status=${encodeURIComponent(status)}` : baseUrl;
    const response = await fetch(url, { next: { revalidate: 15 } });
    if (!response.ok) {
      return [];
    }
    const data = await parseResponse<ApiAuction[]>(response);
    return Array.isArray(data) ? data.map(mapApiToUi) : [];
  }

  async getById(id: string): Promise<UiAuction | null> {
    const url = buildServiceUrl('AUCTION', `/${encodeURIComponent(id)}`);
    const response = await fetch(url, { next: { revalidate: 5 } });
    if (!response.ok) {
      return null;
    }
    const data = await parseResponse<ApiAuction>(response);
    return data ? mapApiToUi(data) : null;
  }

  async updateAuction(id: string, payload: UpdateAuctionPayload): Promise<void> {
    const url = buildServiceUrl('AUCTION', `/${encodeURIComponent(id)}`);
    await this.apiClient.put(url, payload);
  }

  async placeBid(auctionId: string, amount: number): Promise<void> {
    const url = buildServiceUrl('AUCTION', `/${encodeURIComponent(auctionId)}/bid`);
    await this.apiClient.post(url, amount, {
      headers: { 'Content-Type': 'application/json' },
    });
  }

  async getMine(includeClosed = true, accessToken?: string): Promise<UiAuction[]> {
    const url = buildServiceUrl('AUCTION', `/mine?includeClosed=${includeClosed}`);
    const data = await this.apiClient.get<ApiAuction[]>(url, { headers: this.extendHeaders(accessToken) });
    return Array.isArray(data) ? data.map(mapApiToUi) : [];
  }

  private extendHeaders(accessToken?: string): HeadersInit | undefined {
    if (!accessToken) {
      return undefined;
    }
    return {
      Authorization: `Bearer ${accessToken}`,
    };
  }
}
