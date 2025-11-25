export interface ApiArt {
  id?: string | number;
  titleNb?: string | null;
  titleEn?: string | null;
  descriptionNb?: string | null;
  descriptionEn?: string | null;
  artist?: string | null;
  sellerDisplayName?: string | null;
  sellerId?: string | null;
  price?: number | null;
  imageUrl?: string | null;
}

export interface UiArt {
  id: string;
  titleNb: string;
  titleEn: string;
  artist: string;
  sellerDisplayName?: string;
  price: number;
  image: string;
  descriptionNb?: string;
  descriptionEn?: string;
}
