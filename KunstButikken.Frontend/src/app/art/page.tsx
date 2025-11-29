import { headers } from 'next/headers';
import { parseRolesFromBearer, hasRole } from '@/features/auth/lib/server-auth';
import { getContainer } from '@/infrastructure/di/container';
import { GetAllArt } from '@/application/useCases/GetAllArt';
import { GetSellerAuctions } from '@/application/useCases/GetSellerAuctions';
import { ensureRole } from '@/application/security/ensureRole';
import type { CurrentUserContext } from '@/application/security/types';
import ArtForSaleClient from './ArtForSaleClient';
import type { UiArt } from '@/features/art/types/art';

export default async function ArtForSalePage() {
  const hdrs = await headers();
  const authHeader = hdrs.get('authorization') || hdrs.get('Authorization');
  const token = authHeader ? authHeader.replace(/^Bearer\s+/i, '') : null;
  const roles = parseRolesFromBearer(token);

  const container = getContainer();
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

  let mine: UiArt[] = [];
  let others: UiArt[] = allArt.filter(a => a.isVerified);

  if (sellerId && hasRole(roles, 'seller')) {
    const sellerAuctions = await getSellerAuctions.execute({ user: currentUser });
    mine = sellerAuctions.mine as UiArt[];
    others = sellerAuctions.others as UiArt[];
  } else if (hasRole(roles, 'buyer')) {
    others = allArt.filter(a => a.isVerified);
  }

  return <ArtForSaleClient mine={mine} others={others} isSeller={hasRole(roles, 'seller')} isBuyer={hasRole(roles, 'buyer')} />;
}
