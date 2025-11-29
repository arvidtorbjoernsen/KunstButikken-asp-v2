// Centralized shared domain types for the frontend UI
// These types are used across multiple features/routes.
// Keep feature-specific types inside their feature to limit blast radius.

export type ApiArt = {
  id: string;
  titleNb: string;
  titleEn: string;
  descriptionNb?: string;
  descriptionEn?: string;
  imageUrl?: string;
  price: number;
  sellerId?: string;
  sellerDisplayName?: string;
  artist?: string;
  status?: number;
  isVerified?: boolean;
  isFeatured?: boolean;
};

export type UiArt = {
  id: string;
  titleNb: string;
  titleEn: string;
  descriptionNb?: string;
  descriptionEn?: string;
  image?: string;
  price: number;
  sellerId?: string;
  sellerDisplayName?: string;
  artist?: string;
  status?: number;
  isVerified?: boolean;
  isFeatured?: boolean;
  createdAt?: string;
};

export type AdminArt = UiArt & {
  isVerified?: boolean;
  isFeatured?: boolean;
  status?: number;
};

export type Seller = {
  id: string;
  userId: string;
  displayName?: string;
  email?: string;
};
