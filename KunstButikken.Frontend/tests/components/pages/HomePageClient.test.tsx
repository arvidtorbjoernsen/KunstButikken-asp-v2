import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import HomePageClient from '@/app/HomePageClient';
import type { UiArt } from '@/features/art/types/art';
import type { UiAuction } from '@/features/auction/types/auction';

jest.mock('@/features/i18n/components/TranslationProvider', () => ({
  useTranslations: () => ({
    t: (key: string) => {
      const map: Record<string, string> = {
        'home.title': 'Welcome',
        'home.subtitle': 'Subtitle',
        'home.viewToggleLabel': 'View Mode',
        'home.viewMine': 'My art',
        'home.viewAll': 'All art',
        'home.myFeatured': 'Your featured art',
        'home.featured': 'Featured',
        'home.myCatalogue': 'Your auctions',
        'home.allArt': 'All artwork',
        'home.myAuctions': 'Your auctions',
        'home.noAuctions': 'You have no auctions yet.',
        'home.noFeatured': 'No featured items to show.',
        'home.noArtwork': 'No artwork available at the moment.',
      };
      return map[key] ?? key;
    },
  }),
}));

describe('HomePageClient', () => {
  const baseArt = (overrides: Partial<UiArt> = {}): UiArt => ({
    id: overrides.id ?? `art-${Math.random()}`,
    titleEn: overrides.titleEn ?? 'Title EN',
    titleNb: overrides.titleNb ?? 'Title NB',
    descriptionEn: 'Desc EN',
    descriptionNb: 'Desc NB',
    price: 100,
    artist: 'Artist',
    sellerDisplayName: 'Seller',
    isFeatured: overrides.isFeatured,
    ...overrides,
  });

  const baseAuction = (overrides: Partial<UiAuction> = {}): UiAuction => ({
    id: overrides.id ?? `auction-${Math.random()}`,
    artId: 'art-1',
    sellerId: 'seller-1',
    startsAt: new Date(),
    endsAt: new Date(Date.now() + 3600_000),
    startingPrice: 100,
    status: 'Open',
    bidsCount: 0,
    isOpen: true,
    isClosed: false,
    reserveMet: false,
    timeLeftMs: 0,
    ...overrides,
  } as UiAuction);

  test('defaults to seller view when isSeller', () => {
    render(
      <HomePageClient
        isSeller
        initialFeatured={[baseArt({ id: 'all-1' })]}
        initialAll={[baseArt({ id: 'all-2' })]}
        sellerFeatured={[baseArt({ id: 'mine-1', isFeatured: true })]}
        sellerArt={[baseArt({ id: 'mine-2' })]}
        sellerAuctions={[baseAuction({ id: 'auction-1' })]}
      />,
    );

    expect(screen.getByRole('button', { name: /My art/i })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getAllByText(/Your featured art/i)[0]).toBeInTheDocument();
    expect(screen.getAllByText(/Your auctions/i)[0]).toBeInTheDocument();
  });

  test('toggle switches to all art view', () => {
    render(
      <HomePageClient
        isSeller
        initialFeatured={[baseArt({ id: 'all-1', titleEn: 'All Featured' })]}
        initialAll={[baseArt({ id: 'all-2', titleEn: 'All Art' })]}
        sellerFeatured={[baseArt({ id: 'mine-1', titleEn: 'Mine Featured' })]}
        sellerArt={[baseArt({ id: 'mine-2', titleEn: 'Mine Art' })]}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /All art/i }));

    expect(screen.getByText('All Featured')).toBeInTheDocument();
    expect(screen.getByText('All Art')).toBeInTheDocument();
  });

  test('shows empty states when seller collections are empty', () => {
    render(
      <HomePageClient
        isSeller
        sellerFeatured={[]}
        sellerArt={[]}
        sellerAuctions={[]}
        initialFeatured={[]}
        initialAll={[]}
      />,
    );

    expect(screen.getByText(/You have no auctions yet/i)).toBeInTheDocument();
    expect(screen.getByText(/No featured items/i)).toBeInTheDocument();
    expect(screen.getByText(/No artwork available/i)).toBeInTheDocument();
  });
});
