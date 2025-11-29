import { ArtRepository } from '../ArtRepository';

describe('ArtRepository', () => {
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

  test('getAll returns mapped array when fetch ok', async () => {
    const apiArts = [
      {
        id: 123,
        titleNb: 'Nb',
        titleEn: 'En',
        descriptionNb: 'dnb',
        descriptionEn: 'den',
        artist: 'A',
        sellerDisplayName: 'S',
        sellerId: 'sid',
        price: 200,
        imageUrl: '/img.png',
        status: 1,
        isVerified: true,
        isFeatured: false,
      },
    ];

    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: true, headers: new Headers({ 'Content-Type': 'application/json' }), json: async () => apiArts });

    const repo = new ArtRepository({} as any);
    const res = await repo.getAll();
    expect(res).toHaveLength(1);
    const a = res[0];
    expect(a.id).toBe('123');
    expect(a.titleNb).toBe('Nb');
    expect(a.titleEn).toBe('En');
    expect(a.artist).toBe('A');
    expect(a.sellerDisplayName).toBe('S');
    expect(a.price).toBe(200);
    expect(a.image).toBe('/img.png');
    expect(a.status).toBe(1);
    expect(a.isVerified).toBe(true);
  });

  test('getAll returns empty array when fetch not ok', async () => {
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: false });
    const repo = new ArtRepository({} as any);
    const res = await repo.getAll();
    expect(res).toEqual([]);
  });

  test('getById returns null when fetch not ok', async () => {
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: false });
    const repo = new ArtRepository({} as any);
    const res = await repo.getById('x');
    expect(res).toBeNull();
  });

  test('getById maps empty api object to Untitled and Unknown Artist', async () => {
    // Return empty object (no fields)
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: true, headers: new Headers({ 'Content-Type': 'application/json' }), json: async () => ({}) });
    const repo = new ArtRepository({} as any);
    const res = await repo.getById('id');
    expect(res).not.toBeNull();
    expect(res?.titleNb).toBe('Untitled');
    expect(res?.artist).toBe('Unknown Artist');
    // id should be string (may be empty)
    expect(typeof res?.id).toBe('string');
  });

  test('getFeatured proxies to fetchCollection with limit', async () => {
    const apiArts = Array.from({ length: 3 }).map((_, i) => ({ id: i + 1 }));
    // @ts-ignore
    global.fetch.mockResolvedValue({ ok: true, headers: new Headers({ 'Content-Type': 'application/json' }), json: async () => apiArts });
    const repo = new ArtRepository({} as any);
    const res = await repo.getFeatured(3);
    expect(res).toHaveLength(3);
  });
});

