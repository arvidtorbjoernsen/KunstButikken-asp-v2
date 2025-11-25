import { parseResponse } from '@/shared/api';
import ArtDetailClient from "./ArtDetailClient";

interface ApiArt {
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
  createdAt?: string;
}

/**
 * Server-side data fetching for art detail page
 */
async function getArtDetail(id: string): Promise<ApiArt | null> {
  const gateway =
    process.env.NEXT_PUBLIC_API_GATEWAY ||
    process.env.NEXT_PUBLIC_API_BASE_URL ||
    process.env.NEXT_PUBLIC_API_ART ||
    "http://localhost:5000";

  const url = `${gateway.replace(/\/$/, "")}/api/art/${id}`;

  try {
    const res = await fetch(url, {
      next: { revalidate: 60 }
    });

    if (!res.ok) {
      console.error(`Failed to fetch art detail: ${res.status}`);
      return null;
    }

    return await parseResponse<ApiArt>(res);
  } catch (err) {
    console.warn("Failed to fetch art detail server-side:", err instanceof Error ? err.message : String(err));
    return null;
  }
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
