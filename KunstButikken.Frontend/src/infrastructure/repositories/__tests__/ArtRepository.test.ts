import 'reflect-metadata';
import { ArtRepository } from '@/infrastructure/repositories/ArtRepository';
import type { ApiClient } from '@/infrastructure/http/types';

describe('ArtRepository', () => {
  const baseUrl = 'http://gateway.test';
  let originalFetch: typeof global.fetch;

  beforeAll(() => {
    originalFetch = global.fetch;
  });

  beforeEach(() => {
    process.env.NEXT_PUBLIC_API_GATEWAY = baseUrl;
  });

  afterEach(() => {
    global.fetch = originalFetch;
    jest.resetAllMocks();
  });

  it('returns mapped featured art', async () => {
    const apiClient = {} as ApiClient;
    const repo = new ArtRepository(apiClient);
    const response = new Response(
      JSON.stringify([
        {
          id: '1',
          titleNb: 'nb',
          titleEn: 'en',
          price: 100,
          imageUrl: 'img.jpg',
          artist: 'Artist',
          status: 2,
          isVerified: true,
          isFeatured: true,
        },
      ]),
      {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      },
    );
    global.fetch = jest.fn().mockResolvedValue(response) as typeof global.fetch;

    const result = await repo.getFeatured(1);

    expect(global.fetch).toHaveBeenCalledWith(
      `${baseUrl}/api/art/featured?limit=1`,
      expect.objectContaining({ next: { revalidate: 60 } }),
    );
    expect(result).toHaveLength(1);
    expect(result[0]).toMatchObject({ id: '1', artist: 'Artist', isFeatured: true });
  });

  it('returns empty array on fetch failure', async () => {
    const apiClient = {} as ApiClient;
    const repo = new ArtRepository(apiClient);
    const response = new Response('nope', { status: 500 });
    global.fetch = jest.fn().mockResolvedValue(response) as typeof global.fetch;

    const result = await repo.getAll();

    expect(result).toEqual([]);
  });
});

