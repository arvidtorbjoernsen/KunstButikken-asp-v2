import ArtDetailClient from './ArtDetailClient';
import { getContainer } from '@/infrastructure/di/container';
import { GetArtById } from '@/application/useCases/GetArtById';
import type { UiArt } from '@/features/art/types/art';

async function getArtDetail(id: string): Promise<UiArt | null> {
  const container = getContainer();
  const useCase = container.resolve(GetArtById);
  return useCase.execute(id);
}

interface PageProps {
  params: Promise<{ id: string }>;
}

/**
 * Art detail page with server-side rendering
 * Fetches art details on the server for better SEO and performance
 */
export default async function ArtDetailPage({ params }: PageProps) {
  const { id } = await params;
  const art = await getArtDetail(id);

  return <ArtDetailClient art={art} />;
}
