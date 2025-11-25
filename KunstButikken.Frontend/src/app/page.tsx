import { parseResponse } from '@/shared/api';
import type { ApiArt, UiArt } from '@/features/art/types/art';
import HomePageClient from './HomePageClient';
import { buildServiceUrl } from '@/shared/config';

/**
 * Server-side data fetching for the home page
 * This function runs on the server and fetches featured art before rendering
 * Similar to Angular's SSR data fetching in resolvers
 */
async function getFeaturedArt(): Promise<UiArt[]> {
  const urlFeatured = buildServiceUrl('ART', '/featured?limit=5');

  try {
    const res = await fetch(urlFeatured, {
      // Revalidate every 60 seconds (ISR - Incremental Static Regeneration)
      next: { revalidate: 60 },
    });

    if (!res.ok) {
      console.error(`Failed to fetch featured art: ${res.status}`);
      return [];
    }

    const data = await parseResponse<ApiArt[]>(res);

    const mapItem = (a: ApiArt): UiArt => ({
      id: String(a.id ?? ''),
      titleNb: a.titleNb ?? 'Untitled',
      titleEn: a.titleEn ?? 'Untitled',
      descriptionNb: a.descriptionNb ?? undefined,
      descriptionEn: a.descriptionEn ?? undefined,
      artist: a.artist ?? (a.sellerId ? a.sellerId.substring(0, 8) : 'Unknown Artist'),
      sellerDisplayName: a.sellerDisplayName ?? undefined,
      price: a.price ?? 0,
      image: a.imageUrl ?? '',
    });

    return Array.isArray(data) ? data.map(mapItem) : [];
  } catch (err) {
    console.warn(
      'Failed to fetch featured art server-side:',
      err instanceof Error ? err.message : String(err),
    );
    return [];
  }
}

async function getAllArt(): Promise<UiArt[]> {
  const url = buildServiceUrl('ART', '/');

  try {
    const res = await fetch(url, { next: { revalidate: 60 } });
    if (!res.ok) {
      console.error(`Failed to fetch all art: ${res.status}`);
      return [];
    }

    const data = await parseResponse<ApiArt[]>(res);

    const mapItem = (a: ApiArt): UiArt => ({
      id: String(a.id ?? ''),
      titleNb: a.titleNb ?? a.titleEn ?? 'Untitled',
      titleEn: a.titleEn ?? a.titleNb ?? 'Untitled',
      descriptionNb: a.descriptionNb ?? undefined,
      descriptionEn: a.descriptionEn ?? undefined,
      artist: a.artist ?? a.sellerDisplayName ?? a.sellerId ?? 'Unknown Artist',
      sellerDisplayName: a.sellerDisplayName ?? undefined,
      price: a.price ?? 0,
      image: a.imageUrl ?? '',
    });

    return Array.isArray(data) ? data.map(mapItem) : [];
  } catch (err) {
    console.warn(
      'Failed to fetch all art server-side:',
      err instanceof Error ? err.message : String(err),
    );
    return [];
  }
}

/**
 * Home page component with server-side rendering
 *
 * This component is a React Server Component that:
 * - Fetches data on the server before rendering (better performance, SEO)
 * - Generates static HTML that can be cached and served quickly
 * - Passes data to client components for interactivity
 *
 * Similar to Angular's SSR where components can fetch data server-side,
 * but with Next.js this is the default behavior for components without 'use client'
 */
export default async function HomePage() {
  // Fetch data server-side before rendering
  const featured = await getFeaturedArt();
  const all = await getAllArt();

  // Pass the fetched data to the client component for rendering
  // The server generates the initial HTML, then client hydrates for interactivity
  return <HomePageClient initialFeatured={featured} initialAll={all} />;
}
