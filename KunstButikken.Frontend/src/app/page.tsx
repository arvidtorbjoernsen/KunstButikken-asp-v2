import HomePageClient from './HomePageClient';
import { GetFeaturedArt } from '@/application/useCases/GetFeaturedArt';
import { GetAllArt } from '@/application/useCases/GetAllArt';
import { createRequestScope } from '@/infrastructure/di/container';
import type { UiArt } from '@/features/art/types/art';

/**
 * Server-side data fetching for the home page
 * This function runs on the server and fetches featured art before rendering
 * Similar to Angular's SSR data fetching in resolvers
 */
async function getFeaturedArt(): Promise<UiArt[]> {
  const container = createRequestScope();
  const getFeaturedArtUseCase = container.resolve(GetFeaturedArt);
  return getFeaturedArtUseCase.execute(5);
}

async function getAllArt(): Promise<UiArt[]> {
  const container = createRequestScope();
  const getAllArtUseCase = container.resolve(GetAllArt);
  return getAllArtUseCase.execute();
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
