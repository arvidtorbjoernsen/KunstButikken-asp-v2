import { createAsyncThunk, createSlice, PayloadAction } from '@reduxjs/toolkit';
import { UiArt } from '@/features/art/types/art';
import { getAllClient } from '@/features/art/api/art-client';

export interface SearchResultsState {
  results: UiArt[];
  loading: boolean;
  error: string | null;
}

const initialState: SearchResultsState = {
  results: [],
  loading: false,
  error: null,
};

// Async thunk for fetching search results
export const fetchSearchResults = createAsyncThunk(
  'searchResults/fetchSearchResults',
  async (query: string, { rejectWithValue }) => {
    try {
      // Fetch all art first
      const allArt = await getAllClient();

      // Perform client-side filtering
      const lowerCaseQuery = query.toLowerCase();
      const filteredArt = allArt.filter(art => {
        const titleNb = art.titleNb?.toLowerCase() || '';
        const titleEn = art.titleEn?.toLowerCase() || '';
        const artist = art.artist?.toLowerCase() || '';
        const sellerDisplayName = art.sellerDisplayName?.toLowerCase() || '';

        return (
          titleNb.includes(lowerCaseQuery) ||
          titleEn.includes(lowerCaseQuery) ||
          artist.includes(lowerCaseQuery) ||
          sellerDisplayName.includes(lowerCaseQuery)
        );
      });

      return filteredArt;
    } catch (error: unknown) {
      const message = error instanceof Error ? error.message : 'Failed to fetch search results';
      return rejectWithValue(message);
    }
  },
);

const searchResultsSlice = createSlice({
  name: 'searchResults',
  initialState,
  reducers: {
    clearSearchResults: (state) => {
      state.results = [];
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchSearchResults.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchSearchResults.fulfilled, (state, action: PayloadAction<UiArt[]>) => {
        state.loading = false;
        state.results = action.payload;
      })
      .addCase(fetchSearchResults.rejected, (state, action) => {
        state.loading = false;
        state.error = typeof action.payload === 'string' ? action.payload : 'Failed to fetch search results';
        state.results = [];
      });
  },
});

export const { clearSearchResults } = searchResultsSlice.actions;
export default searchResultsSlice.reducer;
