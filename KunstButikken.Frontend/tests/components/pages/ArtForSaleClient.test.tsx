import React from 'react';
import { render, screen } from '@testing-library/react';
import ArtForSaleClient from '@/app/art/ArtForSaleClient';
import type { UiArt } from '@/features/art/types/art';
import type { UiAuction } from '@/features/auction/types/auction';

jest.mock('@/features/i18n/components/TranslationProvider', () => ({
  __esModule: true,
  useTranslations: () => ({
    t: (key: string) => {
      const map: Record<string, string> = {
        'artPage.title': 'Art for Sale',
        'artPage.sellerDescription': 'Seller view',
        'artPage.buyerDescription': 'Buyer view',
        'artPage.defaultDescription': 'Default view',
        'artPage.yourArt': 'Your Art',
        'artPage.otherArt': 'Other Art',
        'artPage.noArt': 'No art available',
        'auction.yourAuctions': 'Your Auctions',
        'auction.none': 'No auctions',
      };
      return map[key] ?? key;
    },
  }),
}));

describe('ArtForSaleClient', () => {
  const sampleArt = (overrides: Partial<UiArt> = {}): UiArt => ({
    id: overrides.id ?? `art-${Math.random()}`,
    titleEn: 'Art EN',
    titleNb: 'Art NB',
    descriptionEn: 'Desc',
    descriptionNb: 'Desc NB',
    price: 100,
    artist: 'Artist',
    sellerDisplayName: 'Seller',
    ...overrides,
  });

  const sampleAuction = (overrides: Partial<UiAuction> = {}): UiAuction => ({
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

  it('renders seller sections in order: auctions, my art, other art', () => {
    const sellerArt = [
      sampleArt({ id: 'art-1', titleEn: 'Featured Piece', titleNb: 'Featured Piece NB', isFeatured: true }),
      sampleArt({ id: 'art-2', titleEn: 'Regular Piece', titleNb: 'Regular Piece NB', isFeatured: false })
    ];
    render(
      <ArtForSaleClient
        isSeller
        myAuctions={[sampleAuction({ id: 'auction-1' })]}
        myArt={sellerArt}
        others={[sampleArt({ id: 'art-3' })]}
      />,
    );

    const sections = screen.getAllByRole('heading', { level: 5 }).map(h => h.textContent);
    expect(sections).toEqual(['Your Auctions', 'Your Art', 'Other Art']);
    const myArtHeadings = screen
      .getAllByRole('heading', { level: 3 })
      .map(h => h.textContent ?? '')
      .filter(title => !title.includes('nav.auctions'));
    expect(myArtHeadings[0]).toContain('Featured Piece');
  });

  it('renders buyer description and other art when not seller', () => {
    render(<ArtForSaleClient isBuyer others={[sampleArt({ id: 'art-3' })]} />);

    expect(screen.getByText('Buyer view')).toBeInTheDocument();
    expect(screen.queryByText('Your Art')).toBeNull();
    expect(screen.getByText('Art for Sale')).toBeInTheDocument();
  });

  it('orders art by featured flag for sellers', () => {
    const artA = sampleArt({ id: 'art-A', titleEn: 'Regular Piece', titleNb: 'Regular Piece NB', isFeatured: false });
    const artB = sampleArt({ id: 'art-B', titleEn: 'Featured Piece', titleNb: 'Featured Piece NB', isFeatured: true });
    render(
      <ArtForSaleClient
        isSeller
        myAuctions={[]}
        myArt={[artA, artB]}
        others={[artA, artB]}
      />,
    );

    const artHeadings = screen
      .getAllByRole('heading', { level: 3 })
      .map(h => h.textContent ?? '')
      .filter(text => text.includes('Piece'));
    expect(artHeadings[0]).toContain('Featured Piece');
    expect(artHeadings[1]).toContain('Regular Piece');
  });

  it('shows empty states when seller has no auctions or art', () => {
    render(<ArtForSaleClient isSeller myAuctions={[]} myArt={[]} others={[]} />);
    expect(screen.getByText('No auctions')).toBeInTheDocument();
    expect(screen.getAllByText('No art available').length).toBeGreaterThanOrEqual(1);
  });
});
