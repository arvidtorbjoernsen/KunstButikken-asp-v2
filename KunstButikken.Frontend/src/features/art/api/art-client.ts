import type { UiArt } from '@/features/art/types/art';
import { createRequestScope } from '@/infrastructure/di/container';
import { GetFeaturedArt } from '@/application/useCases/GetFeaturedArt';
import { GetAllArt } from '@/application/useCases/GetAllArt';
import { GetArtById } from '@/application/useCases/GetArtById';

export async function getFeaturedClient(limit = 5): Promise<UiArt[]> {
  const useCase = createRequestScope().resolve(GetFeaturedArt);
  return useCase.execute(limit);
}

export async function getAllClient(searchQuery?: string): Promise<UiArt[]> {
  const useCase = createRequestScope().resolve(GetAllArt);
  const all = await useCase.execute();
  if (!searchQuery) return all;
  const query = searchQuery.toLowerCase();
  return all.filter(art => {
    const titleNb = art.titleNb?.toLowerCase() ?? '';
    const titleEn = art.titleEn?.toLowerCase() ?? '';
    const artist = art.artist?.toLowerCase() ?? '';
    const seller = art.sellerDisplayName?.toLowerCase() ?? '';
    return (
      titleNb.includes(query) ||
      titleEn.includes(query) ||
      artist.includes(query) ||
      seller.includes(query)
    );
  });
}

export async function getByIdClient(id: string): Promise<UiArt | null> {
  if (!id) return null;
  const useCase = createRequestScope().resolve(GetArtById);
  return useCase.execute(id);
}
