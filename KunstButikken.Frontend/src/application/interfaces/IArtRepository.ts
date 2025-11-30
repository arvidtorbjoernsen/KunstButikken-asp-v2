import type { UiArt } from '@/features/art/types/art';

export interface IArtRepository {
  getFeatured(limit?: number): Promise<UiArt[]>;
  getAll(): Promise<UiArt[]>;
  getById(id: string): Promise<UiArt | null>;
  getMine(includeUnverified?: boolean, accessToken?: string): Promise<UiArt[]>;
}
