import 'reflect-metadata';
import reducer, {
  clearSearchResults,
  fetchSearchResults,
  type SearchResultsState,
} from '../searchResultsSlice';
import type { UiArt } from '@/features/art/types/art';
import * as artClient from '@/features/art/api/art-client';
import { configureStore } from '@reduxjs/toolkit';

jest.mock('@/features/art/api/art-client');

function createTestStore(preloadedState?: Partial<SearchResultsState>) {
  return configureStore({
    reducer: { searchResults: reducer },
    preloadedState: { searchResults: { results: [], loading: false, error: null, ...preloadedState } },
  });
}

describe('searchResultsSlice', () => {
  const mockArt: UiArt[] = [
    { id: '1', titleNb: 'Blomster', titleEn: 'Flowers', price: 100 },
    { id: '2', titleNb: 'Himmel', titleEn: 'Sky', price: 200, artist: 'Sky Painter' },
  ];

  beforeEach(() => {
    jest.resetAllMocks();
  });

  it('clears results via clearSearchResults', () => {
    const initial: SearchResultsState = {
      results: mockArt,
      loading: false,
      error: 'error',
    };
    const state = reducer(initial, clearSearchResults());
    expect(state.results).toEqual([]);
    expect(state.error).toBeNull();
  });

  it('fetchSearchResults fulfilled updates results', async () => {
    (artClient.getAllClient as jest.Mock).mockResolvedValue(mockArt);
    const store = createTestStore();
    await store.dispatch(fetchSearchResults('sky'));

    const state = store.getState().searchResults;
    expect(state.loading).toBe(false);
    expect(state.results).toHaveLength(1);
    expect(state.results[0].titleEn).toBe('Sky');
    expect(state.error).toBeNull();
  });

  it('fetchSearchResults rejected handles errors', async () => {
    (artClient.getAllClient as jest.Mock).mockRejectedValue(new Error('boom'));
    const store = createTestStore();
    await store.dispatch(fetchSearchResults('x'));

    const state = store.getState().searchResults;
    expect(state.loading).toBe(false);
    expect(state.results).toEqual([]);
    expect(state.error).toBe('boom');
  });
});

