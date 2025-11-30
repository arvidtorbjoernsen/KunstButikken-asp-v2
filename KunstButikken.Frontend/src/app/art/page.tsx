import { headers } from 'next/headers';
import { parseRolesFromBearer, hasRole } from '@/features/auth/lib/server-auth';
import { createRequestScope } from '@/infrastructure/di/container';
import { GetAllArt } from '@/application/useCases/GetAllArt';
import { GetSellerAuctions } from '@/application/useCases/GetSellerAuctions';
import { ensureRole } from '@/application/security/ensureRole';
import type { CurrentUserContext } from '@/application/security/types';
import ArtForSaleClient from './ArtForSaleClient';
import type { UiArt } from '@/features/art/types/art';
import type { UiAuction } from '@/features/auction/types/auction';

export default async function ArtForSalePage() {
  const hdrs = await headers();
  const authHeader = hdrs.get('authorization') || hdrs.get('Authorization');
  const token = authHeader ? authHeader.replace(/^Bearer\s+/i, '') : null;
  const roles = parseRolesFromBearer(token);

  const container = createRequestScope();
  const getAllArt = container.resolve(GetAllArt);
  const getSellerAuctions = container.resolve(GetSellerAuctions);
  const allArt = await getAllArt.execute();

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

  const currentUser: CurrentUserContext = {
    id: sellerId,
    roles,
  };

  ensureRole(currentUser, ['buyer', 'seller'], { allowGuests: true });

  const isSellerRole = hasRole(roles, 'seller');
  const isBuyerRole = hasRole(roles, 'buyer');

  const sortArtByFeatured = (items: UiArt[]) =>
    [...items].sort((a, b) => Number(Boolean(b.isFeatured)) - Number(Boolean(a.isFeatured)));

  const sellerArt = sellerId ? sortArtByFeatured(allArt.filter(a => a.sellerId === sellerId)) : [];
  const publicArt = sortArtByFeatured(allArt.filter(a => a.isVerified && (!sellerId || a.sellerId !== sellerId)));

  let myAuctions: UiAuction[] = [];
  if (sellerId && isSellerRole) {
    const sellerAuctions = await getSellerAuctions.execute({ user: currentUser });
    myAuctions = sellerAuctions.mine;
  }

  return (
    <ArtForSaleClient
      myArt={isSellerRole ? sellerArt : []}
      others={publicArt}
      myAuctions={myAuctions}
      isSeller={isSellerRole}
      isBuyer={isBuyerRole}
    />
  );
}
