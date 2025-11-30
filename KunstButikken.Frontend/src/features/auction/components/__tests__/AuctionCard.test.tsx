import React from 'react';
import { render, screen } from '@testing-library/react';
import AuctionCard from '../AuctionCard';
import type { UiAuction } from '@/features/auction/types/auction';

// Mock router interactions that AuctionCard triggers
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
  }),
}));

// Mock Link to avoid Next.js dependency in tests
jest.mock('next/link', () => ({
  __esModule: true,
  default: ({ children, href, ...props }: { children: React.ReactNode; href: string }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

// Mock translation provider
jest.mock('@/features/i18n/components/TranslationProvider', () => ({
  __esModule: true,
  useTranslations: () => ({
    t: (key: string) => {
      const dict: Record<string, string> = {
        'nav.auctions': 'Auctions',
        'auction.open': 'Open',
        'auction.closed': 'Closed',
        'auction.draft': 'Draft',
        'auction.noReserve': 'No reserve',
        'auction.reserveMet': 'Reserve met',
        'auction.reserveNotMet': 'Reserve not met',
        'auction.bids': 'Bids',
        'auction.top': 'Top',
        'auction.highestBid': 'Highest bid',
        'auction.reserve': 'Reserve',
        'auction.reserveMetStatus': 'Met',
        'auction.reserveNotMetStatus': 'Not met',
        'auction.endsIn': 'Ends in',
        'auction.viewArtwork': 'View artwork',
        'art.artist': 'Artist',
        'art.seller': 'Seller',
        'art.price': 'Price',
      };
      return dict[key] ?? key;
    },
    locale: 'en',
  }),
}));

const baseAuction: UiAuction = {
  id: 'auction-123',
  artId: 'art-123',
  sellerId: 'seller-1',
  startsAt: new Date(Date.now() - 3600_000),
  endsAt: new Date(Date.now() + 3600_000),
  startingPrice: 1000,
  reservePrice: 1500,
  status: 'Open',
  bidsCount: 5,
  highestBid: 1600,
  winningBid: null,
  winnerId: null,
  isOpen: true,
  isClosed: false,
  reserveMet: true,
  timeLeftMs: 3600_000,
  artImage: '/art.jpg',
  artTitleNb: 'Kunst NB',
  artTitleEn: 'Art EN',
  artist: 'Painter',
  sellerDisplayName: 'Gallery',
  descriptionNb: 'Desc NB',
  descriptionEn: 'Desc EN',
};

describe('AuctionCard', () => {
  it('renders key auction metadata', () => {
    render(<AuctionCard auction={baseAuction} showStatus />);

    expect(screen.getByText(/art en/i)).toBeInTheDocument();
    expect(screen.getByText(/artist: painter/i)).toBeInTheDocument();
    expect(screen.getByText(/seller: gallery/i)).toBeInTheDocument();
    expect(screen.getAllByText(/price:/i)[0]).toBeInTheDocument();

    expect(screen.getByText(/open/i)).toBeInTheDocument();
    expect(screen.getByText(/reserve met/i)).toBeInTheDocument();

    const bidChips = screen.getAllByText(/bids: 5/i);
    expect(bidChips.length).toBeGreaterThan(0);
    expect(screen.getByText(/top:/i)).toBeInTheDocument();
  });

  it('omits reserve chip when reserve price is not set', () => {
    const auction = { ...baseAuction, reservePrice: undefined, reserveMet: false };
    render(<AuctionCard auction={auction} showStatus />);

    expect(screen.getByText(/no reserve/i)).toBeInTheDocument();
  });

  it('hides highest bid chip when there is no bid yet', () => {
    const auction = { ...baseAuction, highestBid: undefined, bidsCount: 0 };
    render(<AuctionCard auction={auction} showStatus />);

    expect(screen.queryByText(/top:/i)).toBeNull();
    expect(screen.getAllByText(/bids: 0/i)[0]).toBeInTheDocument();
  });
});
