import React from 'react';
import { render, screen } from '@testing-library/react';
import ArtCard from '../ArtCard';

// Mock useTranslations hook
jest.mock('@/features/i18n/components/TranslationProvider', () => ({
  useTranslations: () => ({ locale: 'nb', t: (k: string) => {
    const map: Record<string,string> = { 'art.artist': 'Artist', 'art.unknownArtist': 'Unknown', 'art.seller': 'Seller', 'art.price': 'Price', 'art.labels.featured': 'Featured' };
    return map[k] ?? k;
  } }),
}));

const baseArt = {
  id: '1',
  titleNb: 'Tittel NB',
  titleEn: 'Title EN',
  descriptionNb: 'Beskrivelse NB',
  descriptionEn: 'Description EN',
  artist: 'Kunstner',
  sellerDisplayName: 'Gallery',
  price: 1234,
  image: '/img.png',
  status: 1,
  isVerified: true,
  isFeatured: true,
};

describe('ArtCard', () => {
  test('renders title, description, artist, seller and price', () => {
    render(<ArtCard art={baseArt as any} showStatus={true} />);

    expect(screen.getByText('Tittel NB')).toBeInTheDocument();
    expect(screen.getByText('Beskrivelse NB')).toBeInTheDocument();
    expect(screen.getByText(/Artist/)).toBeInTheDocument();
    expect(screen.getByText(/Seller/)).toBeInTheDocument();
    // Price label
    const priceNode = screen.getByText(/Price:/);
    expect(priceNode).toBeInTheDocument();
    // price should include NOK or digits
    expect(/NOK|\d/.test(priceNode.textContent || '')).toBeTruthy();

    // Status chips: Published, Verified, Featured
    expect(screen.getByText('Published')).toBeInTheDocument();
    expect(screen.getByText('Verified')).toBeInTheDocument();
    expect(screen.getByText('Featured')).toBeInTheDocument();
  });

  test('handles missing image and missing price', () => {
    const art = { ...baseArt, image: undefined, price: undefined } as any;
    render(<ArtCard art={art} showStatus={false} />);

    // without showStatus, chips should not appear
    expect(screen.queryByText('Published')).toBeNull();

    // description present
    expect(screen.getByText('Beskrivelse NB')).toBeInTheDocument();

    // price placeholder '—' should be shown exactly in one element
    const priceNode = screen.getAllByText(/Price:/)[0];
    expect(priceNode).toBeInTheDocument();
    expect(priceNode.textContent).toContain('—');
  });

  test('shows featured chip even when status chips are hidden', () => {
    render(<ArtCard art={baseArt as any} showStatus={false} alwaysShowFeatured />);

    expect(screen.getByText('Featured')).toBeInTheDocument();
    expect(screen.queryByText('Published')).toBeNull();
    expect(screen.queryByText('Verified')).toBeNull();
  });

  test('does not render featured chip when art is not featured', () => {
    const art = { ...baseArt, isFeatured: false } as any;
    render(<ArtCard art={art} showStatus />);

    expect(screen.queryByText('Featured')).toBeNull();
  });
});
