// Add reflect-metadata to satisfy tsyringe during tests
import 'reflect-metadata';

// Tests for src/features/art/api/art-client.ts

jest.resetModules();

// Mock the DI container before importing the module under test
const mockResolve = jest.fn();

jest.mock('@/infrastructure/di/container', () => ({
  getContainer: () => ({ resolve: mockResolve }),
}));

import { getFeaturedClient, getAllClient, getByIdClient } from '@/features/art/api/art-client';

import type { UiArt } from '@/features/art/types/art';

describe('art-client', () => {
  beforeEach(() => {
    mockResolve.mockReset();
  });

  test('getFeaturedClient calls GetFeaturedArt.execute with default and custom limit', async () => {
    const fake = [{ id: '1' } as UiArt, { id: '2' } as UiArt, { id: '3' } as UiArt];
    mockResolve.mockImplementation((cls: any) => {
      if (cls.name === 'GetFeaturedArt') return { execute: (limit: number) => Promise.resolve(fake.slice(0, limit)) };
      return { execute: () => Promise.resolve([]) };
    });

    const resDefault = await getFeaturedClient();
    expect(resDefault).toHaveLength(3 <= 5 ? 3 : 5);

    const resTwo = await getFeaturedClient(2);
    expect(resTwo).toHaveLength(2);
  });

  test('getAllClient returns all when no searchQuery and filters correctly when provided', async () => {
    const arts: UiArt[] = [
      { id: '1', titleNb: 'Blomst', titleEn: 'Flower', artist: 'Anna', sellerDisplayName: 'Gallery A' } as UiArt,
      { id: '2', titleNb: 'Hus', titleEn: 'House', artist: 'Bjørn', sellerDisplayName: 'Gallery B' } as UiArt,
      { id: '3', titleNb: 'Sjø', titleEn: 'Sea', artist: 'Clara', sellerDisplayName: 'Sea Sellers' } as UiArt,
    ];

    mockResolve.mockImplementation((cls: any) => {
      if (cls.name === 'GetAllArt') return { execute: () => Promise.resolve(arts) };
      return { execute: () => Promise.resolve(null) };
    });

    const all = await getAllClient();
    expect(all).toEqual(arts);

    // Search by titleNb
    const byNb = await getAllClient('blomst');
    expect(byNb).toHaveLength(1);
    expect(byNb[0].id).toBe('1');

    // Search by titleEn (case-insensitive)
    const byEn = await getAllClient('HOUSE');
    expect(byEn).toHaveLength(1);
    expect(byEn[0].id).toBe('2');

    // Search by artist
    const byArtist = await getAllClient('clara');
    expect(byArtist).toHaveLength(1);
    expect(byArtist[0].id).toBe('3');

    // Search by seller
    const bySeller = await getAllClient('gallery');
    expect(bySeller).toHaveLength(2);
  });

  test('getByIdClient returns null for empty id and resolves use case otherwise', async () => {
    mockResolve.mockImplementation((cls: any) => {
      if (cls.name === 'GetArtById') return { execute: (id: string) => Promise.resolve({ id, titleNb: 'X' } as UiArt) };
      return { execute: () => Promise.resolve(null) };
    });

    const nullRes = await getByIdClient('');
    expect(nullRes).toBeNull();

    const art = await getByIdClient('abc');
    expect(art).not.toBeNull();
    expect((art as UiArt).id).toBe('abc');
  });
});
