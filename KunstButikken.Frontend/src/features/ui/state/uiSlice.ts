import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export type UIState = {
  mobileDrawerOpen: boolean;
  mobileSearchOpen: boolean;
};

const initialState: UIState = {
  mobileDrawerOpen: false,
  mobileSearchOpen: false,
};

const uiSlice = createSlice({
  name: 'ui',
  initialState,
  reducers: {
    setMobileDrawerOpen(state, action: PayloadAction<boolean>) {
      state.mobileDrawerOpen = action.payload;
    },
    toggleMobileDrawer(state) {
      state.mobileDrawerOpen = !state.mobileDrawerOpen;
    },
    setMobileSearchOpen(state, action: PayloadAction<boolean>) {
      state.mobileSearchOpen = action.payload;
    },
    toggleMobileSearch(state) {
      state.mobileSearchOpen = !state.mobileSearchOpen;
    },
  },
});

export const { setMobileDrawerOpen, toggleMobileDrawer, setMobileSearchOpen, toggleMobileSearch } = uiSlice.actions;
export default uiSlice.reducer;
