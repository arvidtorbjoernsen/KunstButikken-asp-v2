import type { UiAuction } from '@/features/auction/types/auction';

export interface IAuctionRepository {
  getAuctions(status?: 'Open' | 'Closed' | 'Draft'): Promise<UiAuction[]>;
  getById(id: string): Promise<UiAuction | null>;
  updateAuction(id: string, payload: UpdateAuctionPayload): Promise<void>;
  placeBid(auctionId: string, amount: number): Promise<void>;
  getMine(includeClosed?: boolean, accessToken?: string): Promise<UiAuction[]>;
}

export type UpdateAuctionPayload = {
  startsAt: string;
  endsAt: string;
  startingPrice: number;
  reservePrice?: number | null;
};
