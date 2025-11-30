import HomePageClient from './HomePageClient';
import { GetFeaturedArt } from '@/application/useCases/GetFeaturedArt';
import { GetAllArt } from '@/application/useCases/GetAllArt';
import { createRequestScope } from '@/infrastructure/di/container';
import type { UiArt } from '@/features/art/types/art';
import { headers } from 'next/headers';
import { parseRolesFromBearer, hasRole } from '@/features/auth/lib/server-auth';
import { Buffer } from 'node:buffer';
import { GetSellerAuctions } from '@/application/useCases/GetSellerAuctions';
import { GetSellerArt } from '@/application/useCases/GetSellerArt';
import type { CurrentUserContext } from '@/application/security/types';
import type { UiAuction } from '@/features/auction/types/auction';
import { cookies } from 'next/headers';
import { createCurrentUserFromToken } from '@/application/security/CurrentUserFactory';

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

const sortArtByFeatured = (items: UiArt[]): UiArt[] =>
  [...items].sort((a, b) => Number(Boolean(b.isFeatured)) - Number(Boolean(a.isFeatured)));

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
  const cookieStore = await cookies();
  const sessionToken = cookieStore.get('kb_session_access')?.value;
  const authHeader = hdrs.get('authorization') || hdrs.get('Authorization');
  const bearerFromHeader = authHeader ? authHeader.replace(/^Bearer\s+/i, '') : null;
  const token = sessionToken ?? bearerFromHeader;
  const currentUser = createCurrentUserFromToken(token);
  const roles = currentUser.roles;

  const featured = sortArtByFeatured(await getFeaturedArt());
  const all = sortArtByFeatured(await getAllArt());

  let sellerId = currentUser.id;

  const isSeller = roles.includes('seller') && !!sellerId;
  const container = createRequestScope();
  let sellerArt: UiArt[] = [];
  let sellerFeatured: UiArt[] = [];
  let sellerAuctions: UiAuction[] = [];

  if (isSeller && sellerId && token) {
    const getSellerArt = container.resolve(GetSellerArt);
    const getSellerAuctions = container.resolve(GetSellerAuctions);
    const currentSeller: CurrentUserContext = { id: sellerId, roles, token };
    try {
      const mineArt = await getSellerArt.execute({ user: currentSeller });
      sellerArt = sortArtByFeatured(mineArt);
      sellerFeatured = sellerArt.filter(a => a.isFeatured);
    } catch {
      sellerArt = [];
      sellerFeatured = [];
    }
    try {
      const auctions = await getSellerAuctions.execute({ user: currentSeller });
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
