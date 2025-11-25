import { configureStore } from '@reduxjs/toolkit';
import uiReducer from '@/features/ui/state/uiSlice';
import searchReducer from '@/features/search/state/searchSlice';
import auctionReducer from '@/features/auction/state/auctionSlice';
import searchResultsReducer from '@/features/search/state/searchResultsSlice'; // Import the new reducer

export const store = configureStore({
  reducer: {
    ui: uiReducer,
    search: searchReducer,
    auction: auctionReducer,
    searchResults: searchResultsReducer, // Add the new reducer
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
