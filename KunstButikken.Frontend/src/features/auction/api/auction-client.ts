import type { ApiAuction, UiAuction } from '../types/auction';

export function mapApiToUi(a: ApiAuction): UiAuction {
  const startsAt = new Date(a.startsAt);
  const endsAt = new Date(a.endsAt);
  const bids = Array.isArray(a.bids) ? a.bids : [];
  const highestBid = bids.length ? Math.max(...bids.map(b => b.amount)) : undefined;
  const now = Date.now();
  const timeLeftMs = endsAt.getTime() - now;
  const hasStarted = startsAt.getTime() <= now;
  const hasEnded = timeLeftMs <= 0;

  // Normalize status: backend may send numeric enum (0/1/2)
  const statusNorm = ((): 'Draft' | 'Open' | 'Closed' => {
    const s = a.status;
    if (typeof s === 'number') return s === 1 ? 'Open' : s === 2 ? 'Closed' : 'Draft';
    if (s === 'Open' || s === 'Closed' || s === 'Draft') return s;
    // Fallback: infer from time
    return hasEnded ? 'Closed' : 'Open';
  })();

  // An auction is open only if: status is 'Open' AND it has started AND it hasn't ended
  const isOpen = statusNorm === 'Open' && hasStarted && !hasEnded;
  const isClosed = statusNorm === 'Closed' || hasEnded || !hasStarted;

  const reservePrice = a.reservePrice ?? undefined;
  const reserveMet = reservePrice == null || (highestBid != null && highestBid >= reservePrice);
  return {
    id: String(a.id),
    artId: String(a.artId),
    sellerId: String(a.sellerId),
    startsAt,
    endsAt,
    startingPrice: a.startingPrice ?? 0,
    reservePrice,
    status: statusNorm,
    bidsCount: bids.length,
    highestBid,
    isOpen,
    isClosed,
    reserveMet,
    timeLeftMs,
    sellerDisplayName: a.sellerDisplayName,
  };
}
