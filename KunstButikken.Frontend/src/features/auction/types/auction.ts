// Centralized types for Auctions feature
// Mirrors AuctionService models with light UI augmentation

export type ApiBid = {
  id: string;
  auctionId: string;
  bidderId: string;
  amount: number;
  placedAt: string; // ISO string
};

export type AuctionStatus = 'Draft' | 'Open' | 'Closed';

export type ApiAuction = {
  id: string;
  artId: string;
  sellerId: string;
  // Include seller display name from backend to avoid extra art lookups
  sellerDisplayName?: string;
  startsAt: string; // ISO
  endsAt: string; // ISO
  startingPrice: number;
  reservePrice?: number | null;
  winningBid?: number | null;
  winnerId?: string | null;
  status: AuctionStatus;
  bids: ApiBid[];
};

export type UiAuction = {
  id: string;
  artId: string;
  sellerId: string;
  startsAt: Date;
  endsAt: Date;
  startingPrice: number;
  reservePrice?: number;
  status: AuctionStatus;
  bidsCount: number;
  highestBid?: number;
  // Include final results so UI can display winners when auction is closed
  winningBid?: number | null;
  winnerId?: string | null;
  // Convenience flags
  isOpen: boolean;
  isClosed: boolean;
  reserveMet: boolean;
  timeLeftMs: number; // negative if ended
  artImage?: string; // optional small image for card thumbnails
  artTitleNb?: string;
  artTitleEn?: string;
  artist?: string;
  sellerDisplayName?: string;
  descriptionNb?: string; // Added descriptionNb
  descriptionEn?: string; // Added descriptionEn
};
