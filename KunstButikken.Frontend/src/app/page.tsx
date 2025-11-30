import HomePageClient from './HomePageClient';
import { GetFeaturedArt } from '@/application/useCases/GetFeaturedArt';
import { GetAllArt } from '@/application/useCases/GetAllArt';
import { createRequestScope } from '@/infrastructure/di/container';
import type { UiArt } from '@/features/art/types/art';
import { headers } from 'next/headers';
import { parseRolesFromBearer, hasRole } from '@/features/auth/lib/server-auth';
import { GetSellerAuctions } from '@/application/useCases/GetSellerAuctions';
import type { CurrentUserContext } from '@/application/security/types';
import type { UiAuction } from '@/features/auction/types/auction';

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
  const hdrs = await headers();
  const authHeader = hdrs.get('authorization') || hdrs.get('Authorization');
  const token = authHeader ? authHeader.replace(/^Bearer\s+/i, '') : null;
  const roles = parseRolesFromBearer(token);

  const featured = await getFeaturedArt();
  const all = await getAllArt();

  let sellerId: string | undefined;
  if (token) {
    try {
      const [, payload] = token.split('.');
      const decoded = JSON.parse(Buffer.from(payload, 'base64url').toString('utf8'));
      sellerId = decoded?.sub;
    } catch {
      sellerId = undefined;
    }
  }

  const isSeller = hasRole(roles, 'seller') && !!sellerId;

  let sellerArt: UiArt[] = [];
  let sellerFeatured: UiArt[] = [];
  let sellerAuctions: UiAuction[] = [];

  if (isSeller && sellerId) {
    sellerArt = all.filter(art => art.sellerId === sellerId);
    sellerFeatured = featured.filter(art => art.sellerId === sellerId);

    const container = createRequestScope();
    const getSellerAuctions = container.resolve(GetSellerAuctions);
    const currentUser: CurrentUserContext = { id: sellerId, roles };
    try {
      const auctions = await getSellerAuctions.execute({ user: currentUser });
      sellerAuctions = auctions.mine;
    } catch {
      sellerAuctions = [];
    }
  }

  return (
    <HomePageClient
      initialFeatured={featured}
      initialAll={all}
      sellerFeatured={sellerFeatured}
      sellerArt={sellerArt}
      sellerAuctions={sellerAuctions}
      isSeller={isSeller}
    />
  );
}
