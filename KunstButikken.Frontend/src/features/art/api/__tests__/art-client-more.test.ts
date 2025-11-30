import 'reflect-metadata';

jest.resetModules();
const mockResolve = jest.fn();
jest.mock('@/infrastructure/di/container', () => ({ createRequestScope: () => ({ resolve: mockResolve }) }));

import { getAllClient, getFeaturedClient } from '@/features/art/api/art-client';

import type { UiArt } from '@/features/art/types/art';

describe('art-client additional branches', () => {
  beforeEach(() => mockResolve.mockReset());

  test('getAllClient handles empty execute result gracefully', async () => {
    // Return empty array instead of null to match runtime behavior
    mockResolve.mockImplementation((cls: any) => ({ execute: () => Promise.resolve([]) }));
    const res = await getAllClient();
    expect(res).toEqual([]);
  });

  test('getAllClient filters when some fields are missing', async () => {
    const arts: UiArt[] = [
      { id: '1', titleNb: undefined as any, titleEn: 'Sun', artist: undefined as any, sellerDisplayName: undefined as any } as UiArt,
      { id: '2', titleNb: 'Måne', titleEn: undefined as any, artist: 'Lars', sellerDisplayName: '' } as UiArt,
    ];
    mockResolve.mockImplementation((cls: any) => {
      if (cls.name === 'GetAllArt') return { execute: () => Promise.resolve(arts) };
      return { execute: () => Promise.resolve([]) };
    });

    // search 'sun' should match titleEn of first
    const bySun = await getAllClient('Sun');
    expect(bySun).toHaveLength(1);
    expect(bySun[0].id).toBe('1');

    // search by artist 'lars'
    const byLars = await getAllClient('lars');
    expect(byLars).toHaveLength(1);
    expect(byLars[0].id).toBe('2');
  });

  test('getFeaturedClient handles fewer items than limit', async () => {
    const fake = [{ id: '1' } as UiArt, { id: '2' } as UiArt];
    mockResolve.mockImplementation((cls: any) => ({ execute: (limit: number) => Promise.resolve(fake.slice(0, limit)) }));

    const res = await getFeaturedClient(5);
    expect(res).toHaveLength(2);
  });
});
