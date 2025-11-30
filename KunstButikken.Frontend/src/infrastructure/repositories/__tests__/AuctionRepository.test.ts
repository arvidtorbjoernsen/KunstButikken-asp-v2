import { AuctionRepository } from '../AuctionRepository';

describe('AuctionRepository', () => {
  const origFetch = global.fetch;
  beforeEach(() => {
    jest.resetModules();
    // @ts-ignore
    global.fetch = jest.fn();
  });
  afterEach(() => {
    // @ts-ignore
    global.fetch = origFetch;
  });

  test('getAuctions returns empty on non-ok', async () => {
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: false });
    const repo = new AuctionRepository({} as any);
    const res = await repo.getAuctions('Open');
    expect(res).toEqual([]);
  });

  test('getById returns null on non-ok', async () => {
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: false });
    const repo = new AuctionRepository({} as any);
    const res = await repo.getById('x');
    expect(res).toBeNull();
  });

  test('updateAuction calls apiClient.put and placeBid calls post', async () => {
    const apiClient = { put: jest.fn().mockResolvedValue(undefined), post: jest.fn().mockResolvedValue(undefined) } as any;
    const repo = new AuctionRepository(apiClient);
    await repo.updateAuction('id', { startingPrice: 10 } as any);
    expect(apiClient.put).toHaveBeenCalled();

    await repo.placeBid('id', 123);
    expect(apiClient.post).toHaveBeenCalled();
  });

  // New tests to improve branch coverage
  test('getAuctions returns mapped array when fetch ok', async () => {
    const apiAuctions = [
      { id: 1, artId: 2, sellerId: 3, startsAt: new Date().toISOString(), endsAt: new Date(Date.now() + 1000).toISOString(), status: 'Open', bids: [], startingPrice: 10 } as any,
    ];
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: true, headers: new Headers({ 'Content-Type': 'application/json' }), json: async () => apiAuctions });
    const repo = new AuctionRepository({} as any);
    const res = await repo.getAuctions();
    expect(res).toHaveLength(1);
    expect(res[0].id).toBe('1');
  });

  test('getAuctions returns empty when response ok but body not array', async () => {
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: true, headers: new Headers({ 'Content-Type': 'application/json' }), json: async () => ({ not: 'array' }) });
    const repo = new AuctionRepository({} as any);
    const res = await repo.getAuctions();
    expect(res).toEqual([]);
  });

  test('getById returns mapped auction when ok', async () => {
    const api = { id: 5, artId: 2, sellerId: 3, startsAt: new Date().toISOString(), endsAt: new Date(Date.now() + 1000).toISOString(), status: 'Open', bids: [] } as any;
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: true, headers: new Headers({ 'Content-Type': 'application/json' }), json: async () => api });
    const repo = new AuctionRepository({} as any);
    const res = await repo.getById('5');
    expect(res).not.toBeNull();
    expect(res?.id).toBe('5');
  });
});
