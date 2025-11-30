import { mapApiToUi } from '../auction-client';

describe('mapApiToUi', () => {
  const realNow = Date.now;
  afterEach(() => {
    Date.now = realNow;
    jest.useRealTimers();
  });

  test('numeric status 1 -> Open when within timeframe and has bids', () => {
    const now = new Date('2025-01-01T12:00:00Z').getTime();
    Date.now = jest.fn(() => now);

    const startsAt = new Date(now - 1000 * 60).toISOString();
    const endsAt = new Date(now + 1000 * 60).toISOString();

    const api = {
      id: 1,
      artId: 2,
      sellerId: 3,
      startsAt,
      endsAt,
      status: 1,
      bids: [{ amount: 100 }, { amount: 200 }],
      startingPrice: 50,
      reservePrice: 150,
      sellerDisplayName: 'S',
    } as any;

    const u = mapApiToUi(api);
    expect(u.status).toBe('Open');
    expect(u.isOpen).toBe(true);
    expect(u.isClosed).toBe(false);
    expect(u.bidsCount).toBe(2);
    expect(u.highestBid).toBe(200);
    expect(u.reserveMet).toBe(true);
  });

  test('numeric status 2 -> Closed regardless of time', () => {
    const now = new Date('2025-01-01T12:00:00Z').getTime();
    Date.now = jest.fn(() => now);

    const startsAt = new Date(now + 1000 * 60).toISOString(); // starts in future
    const endsAt = new Date(now + 1000 * 120).toISOString();

    const api = { id: 1, artId: 2, sellerId: 3, startsAt, endsAt, status: 2, bids: [], startingPrice: 0 } as any;
    const u = mapApiToUi(api);
    expect(u.status).toBe('Closed');
    expect(u.isOpen).toBe(false);
    expect(u.isClosed).toBe(true);
  });

  test('string status Draft and ended inference -> Closed', () => {
    const now = new Date('2025-01-02T12:00:00Z').getTime();
    Date.now = jest.fn(() => now);

    const startsAt = new Date(now - 1000 * 3600).toISOString();
    const endsAt = new Date(now - 1000 * 60).toISOString(); // already ended

    const api = { id: 1, artId: 2, sellerId: 3, startsAt, endsAt, status: 'Draft', bids: [] } as any;
    const u = mapApiToUi(api);
    expect(u.status).toBe('Draft');
    // Because ended -> isClosed true
    expect(u.isClosed).toBe(true);
    expect(u.isOpen).toBe(false);
  });

  test('non-array bids -> treated as empty, highestBid undefined', () => {
    const now = Date.now();
    Date.now = jest.fn(() => now);

    const startsAt = new Date(now - 1000).toISOString();
    const endsAt = new Date(now + 1000).toISOString();

    const api = { id: 1, artId: 2, sellerId: 3, startsAt, endsAt, status: 'Open', bids: null } as any;
    const u = mapApiToUi(api);
    expect(u.bidsCount).toBe(0);
    expect(u.highestBid).toBeUndefined();
  });

  test('reservePrice absent -> reserveMet true', () => {
    const now = Date.now();
    Date.now = jest.fn(() => now);
    const startsAt = new Date(now - 1000).toISOString();
    const endsAt = new Date(now + 1000).toISOString();

    const api = { id: 1, artId: 2, sellerId: 3, startsAt, endsAt, status: 'Open', bids: [{ amount: 10 }] } as any;
    const u = mapApiToUi(api);
    expect(u.reservePrice).toBeUndefined();
    expect(u.reserveMet).toBe(true);
  });

  test('fallback status inference when unknown status and not ended -> Open', () => {
    const now = new Date('2025-06-01T12:00:00Z').getTime();
    Date.now = jest.fn(() => now);
    const startsAt = new Date(now - 1000 * 60).toISOString();
    const endsAt = new Date(now + 1000 * 60).toISOString();

    const api = { id: 1, artId: 2, sellerId: 3, startsAt, endsAt, status: 'weird' as any, bids: [] } as any;
    const u = mapApiToUi(api);
    expect(u.status).toBe('Open');
  });
});

