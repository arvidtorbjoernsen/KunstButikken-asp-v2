import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { UiAuction } from '../types/auction';

// A serializable version of UiAuction where Date objects are strings.
export type SerializableUiAuction = Omit<UiAuction, 'startsAt' | 'endsAt'> & {
  startsAt: string;
  endsAt: string;
};

interface AuctionState {
  auction: SerializableUiAuction | null;
  error: string | null;
}

const initialState: AuctionState = {
  auction: null,
  error: null,
};

const auctionSlice = createSlice({
  name: 'auction',
  initialState,
  reducers: {
    // This action now expects a payload that is already serializable.
    setAuction(state, action: PayloadAction<SerializableUiAuction>) {
      state.auction = action.payload;
      state.error = null;
    },
    setError(state, action: PayloadAction<string | null>) {
      state.error = action.payload;
    },
  },
});

export const { setAuction, setError } = auctionSlice.actions;
export default auctionSlice.reducer;
