import React from 'react';
import { render, screen } from '@testing-library/react';

// Mock the translation provider used by the component
jest.mock('@/features/i18n/components/TranslationProvider', () => ({
  __esModule: true,
  useTranslations: () => ({
    t: (key: string) => {
      // provide predictable translations for the test keys we assert
      const map: Record<string, string> = {
        'art.artist': 'Artist',
        'art.unknownArtist': 'Unknown',
        'art.seller': 'Seller',
        'art.price': 'Price',
        'art.labels.featured': 'Featured',
      };
      return map[key] ?? key;
    },
    locale: 'en', // default to English for tests
  }),
}));

import ArtCard from '@/features/art/components/ArtCard';
import type { UiArt } from '@/features/art/types/art';

const baseArt: Partial<UiArt> = {
  id: 'art-1',
  titleEn: 'Sunset Over Sea',
  titleNb: 'Solnedgang over havet',
  descriptionEn: 'A beautiful painting',
  descriptionNb: 'Et vakkert maleri',
  artist: 'Jane Doe',
  sellerDisplayName: 'Gallery One',
  price: 250,
  image: '/sunset.jpg',
};

describe('ArtCard', () => {
  it('renders title, description, artist, seller and price and links to art page', () => {
    render(<ArtCard art={baseArt as UiArt} />);

    // Title
    expect(screen.getByRole('heading', { name: /sunset over sea/i })).toBeInTheDocument();

    // Description
    expect(screen.getByText(/a beautiful painting/i)).toBeInTheDocument();

    // Artist and seller (captions are rendered as text)
    expect(screen.getByText(/artist: jane doe/i)).toBeInTheDocument();
    expect(screen.getByText(/seller: gallery one/i)).toBeInTheDocument();

    // Price (rendered in NOK formatting in the app)
    expect(
      screen.getByText(
        content => /price/i.test(content) && /nok/i.test(content) && /250/.test(content),
      ),
    ).toBeInTheDocument();

    // Link href
    const link = screen.getByRole('link') as HTMLAnchorElement;
    expect(link).toHaveAttribute('href', '/art/art-1');

    // Image alt and src
    const img = screen.getByRole('img', { name: /sunset over sea/i }) as HTMLImageElement;
    expect(img).toBeInTheDocument();
    expect(img.src).toContain('/sunset.jpg');
  });

  it('uses placeholder image when image is missing', () => {
    const artNoImage = { ...baseArt, id: 'art-2', image: undefined };
    render(<ArtCard art={artNoImage as UiArt} />);

    const img = screen.getByRole('img', { name: /sunset over sea/i }) as HTMLImageElement;
    // JSDOM resolves relative src to absolute file:// or http://localhost-like url in tests; assert using endsWith
    expect(img.src).toMatch(/placeholder-600x400.svg$/);
  });

  it('shows unknown artist when artist is missing', () => {
    const artNoArtist = { ...baseArt, id: 'art-3', artist: undefined };
    render(<ArtCard art={artNoArtist as UiArt} />);

    expect(screen.getByText(/artist: unknown/i)).toBeInTheDocument();
  });

  it('can show featured chip without status chips when requested', () => {
    const featuredArt = { ...baseArt, id: 'art-4', isFeatured: true };
    render(<ArtCard art={featuredArt as UiArt} alwaysShowFeatured />);

    expect(screen.getByText(/featured/i)).toBeInTheDocument();
    expect(screen.queryByText(/published/i)).toBeNull();
  });

  it('hides featured chip when art is not featured', () => {
    const nonFeatured = { ...baseArt, id: 'art-5', isFeatured: false };
    render(<ArtCard art={nonFeatured as UiArt} showStatus />);

    expect(screen.queryByText(/featured/i)).toBeNull();
  });
});
