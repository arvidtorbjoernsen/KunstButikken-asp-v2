// Centralized design tokens for spacing, radii, layout sizes, etc.
export const tokens = {
  // Base spacing unit (px)
  base: 4,
  spacing(n: number) { return n * this.base; },
  space: {
    xs: 4,
    sm: 8,
    md: 12,
    lg: 16,
    xl: 24,
  },
  radii: {
    sm: 8,
    md: 12,
    lg: 16,
    pill: 9999,
  },
  layout: {
    navHeight: { xs: 56, sm: 60 },
    containerMaxWidth: 1280,
  },
};

export type Tokens = typeof tokens;

