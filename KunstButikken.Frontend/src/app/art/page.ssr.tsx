// Server component variant for role-filtered art list (SSR)
import { headers } from 'next/headers';
import { parseRolesFromBearer, hasRole } from '@/features/auth/lib/server-auth';
import { getAllServer } from '@/features/art/api/art';
import ArtForSaleClient from './ArtForSaleClient';
import type { UiArt } from '@/features/art/types/art';

function splitBySeller(arts: UiArt[], sellerId: string | undefined) {
  if (!sellerId) return { mine: [], others: arts };
  const mine = arts.filter(a => a.sellerId === sellerId);
  const others = arts.filter(a => a.sellerId !== sellerId);
  return { mine, others };
}

export default async function ArtForSalePageSSR() {
  const hdrs = await headers();
  const authHeader = hdrs.get('authorization') || hdrs.get('Authorization');
  const token = authHeader ? authHeader.replace(/^Bearer\s+/i, '') : null;
  const roles = parseRolesFromBearer(token);
  const isSeller = hasRole(roles, 'seller');
  const isBuyer = hasRole(roles, 'buyer');

  const all = await getAllServer();

  let sellerId: string | undefined;
  if (token) {
    try {
      const [, payload] = token.split('.');
      const decoded = JSON.parse(Buffer.from(payload, 'base64url').toString('utf8'));
      sellerId = decoded?.sub;
    } catch {}
  }

  let mine: UiArt[] = [];
  let others: UiArt[] = [];

  if (isSeller && sellerId) {
    const split = splitBySeller(all, sellerId);
    mine = split.mine;
    others = split.others;
  } else if (isBuyer) {
    // Buyers see only verified art
    others = all.filter(a => a.isVerified);
  } else {
    // Guests behave like buyers but cannot buy
    others = all.filter(a => a.isVerified);
  }

  return <ArtForSaleClient mine={mine} others={others} isSeller={isSeller} isBuyer={isBuyer} />;
}
