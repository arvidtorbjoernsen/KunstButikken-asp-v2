import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';

// Mock translations
jest.mock('@/features/i18n/components/TranslationProvider', () => ({
  __esModule: true,
  useTranslations: () => ({
    t: (k: string) => ({ 'home.searchPlaceholder': 'Search...', 'nav.search': 'Search' })[k] ?? k,
  }),
}));

// Provide a mock dispatch and selector implementation
const mockDispatch = jest.fn();
jest.mock('react-redux', () => ({
  __esModule: true,
  useDispatch: () => mockDispatch,
  useSelector: (selector: (state: any) => unknown) =>
    selector({
      search: { query: 'initial' },
      searchResults: { results: [], loading: false, error: null },
    }),
}));

import SearchOverlay from '@/features/navigation/components/SearchOverlay';
import { setQuery } from '@/features/search/state/searchSlice';

describe('NavbarSearch', () => {
  beforeEach(() => {
    mockDispatch.mockClear();
  });

  it('renders input with initial query and dispatches setQuery on change', () => {
    render(<SearchOverlay open={true} onClose={() => {}} anchorEl={document.body} />);

    const input = screen.getByRole('textbox', { name: /search/i }) as HTMLInputElement;
    expect(input).toBeInTheDocument();
    expect(input.value).toBe('initial');

    // Simulate a single change event with the desired final value.
    fireEvent.change(input, { target: { value: 'flowers' } });

    // Ensure dispatch was called with an action matching setQuery('flowers') at least once
    const dispatchedHasFlowers = mockDispatch.mock.calls.some(call => {
      const arg = call[0];
      return arg && arg.type === setQuery('flowers').type && arg.payload === 'flowers';
    });

    expect(dispatchedHasFlowers).toBe(true);
  });

  it('renders placeholder and search button text from translations', () => {
    render(<SearchOverlay open={true} onClose={() => {}} anchorEl={document.body} />);

    const input = screen.getByRole('textbox', { name: /search/i });
    expect(input).toHaveAttribute('placeholder', 'Search...');

    // There are multiple buttons with accessible name 'search' (icon + submit). Use visible text for the submit button.
    const submitBtn = screen.getByRole('button', { name: /search/i });
    expect(submitBtn).toBeInTheDocument();
  });
});
