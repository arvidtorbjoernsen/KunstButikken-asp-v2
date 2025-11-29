import 'reflect-metadata';
import { AuctionRepository } from '@/infrastructure/repositories/AuctionRepository';
import type { ApiClient } from '@/infrastructure/http/types';
import type { ApiAuction } from '@/features/auction/types/auction';

const sampleApiAuction: ApiAuction = {
  id: 'a1',
  artId: 'art-1',
  sellerId: 'seller-1',
  startsAt: new Date().toISOString(),
  endsAt: new Date(Date.now() + 3600_000).toISOString(),
  startingPrice: 100,
  status: 'Open',
  bids: [],
};

describe('AuctionRepository', () => {
  const baseUrl = 'http://gateway.test';
  let originalFetch: typeof global.fetch;
  let apiClient: jest.Mocked<ApiClient>;

  beforeAll(() => {
    originalFetch = global.fetch;
  });

  beforeEach(() => {
    process.env.NEXT_PUBLIC_API_GATEWAY = baseUrl;
    apiClient = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn(),
      patch: jest.fn(),
    } as unknown as jest.Mocked<ApiClient>;
  });

  afterEach(() => {
    global.fetch = originalFetch;
    jest.resetAllMocks();
  });

  it('fetches auctions and maps results', async () => {
    const repo = new AuctionRepository(apiClient);
    const response = new Response(JSON.stringify([sampleApiAuction]), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
    global.fetch = jest.fn().mockResolvedValue(response) as typeof global.fetch;

    const result = await repo.getAuctions('Open');

    expect(global.fetch).toHaveBeenCalledWith(
      `${baseUrl}/api/auctions/?status=Open`,
      expect.objectContaining({ next: { revalidate: 15 } }),
    );
    expect(result).toHaveLength(1);
    expect(result[0]).toMatchObject({ id: 'a1', artId: 'art-1' });
  });

  it('returns null when getById fails', async () => {
    const repo = new AuctionRepository(apiClient);
    global.fetch = jest.fn().mockResolvedValue(new Response('nope', { status: 404 })) as typeof global.fetch;

    const result = await repo.getById('a1');
    expect(result).toBeNull();
  });

  it('calls apiClient.put for updateAuction', async () => {
    const repo = new AuctionRepository(apiClient);
    const payload = {
      startsAt: sampleApiAuction.startsAt,
      endsAt: sampleApiAuction.endsAt,
      startingPrice: 120,
      reservePrice: 150,
    };

    await repo.updateAuction('a1', payload);

    expect(apiClient.put).toHaveBeenCalledWith(`${baseUrl}/api/auctions/a1`, payload);
  });

  it('calls apiClient.post for placeBid', async () => {
    const repo = new AuctionRepository(apiClient);
    await repo.placeBid('a1', 250);

    expect(apiClient.post).toHaveBeenCalledWith(
      `${baseUrl}/api/auctions/a1/bid`,
      250,
      expect.objectContaining({ headers: { 'Content-Type': 'application/json' } }),
    );
  });
});
