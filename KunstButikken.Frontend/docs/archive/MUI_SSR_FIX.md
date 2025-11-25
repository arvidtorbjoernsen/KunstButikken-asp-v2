# MUI SSR Styling Fix for Next.js 16

## Problem
The navbar and other MUI components were losing their styling after page reload/restart. Styles would be overwritten and not persist correctly between server-side rendering and client-side hydration.

## Root Cause
The issue was caused by improper Emotion cache configuration for server-side rendering in Next.js 16 with MUI 7. The custom `EmotionRegistry` implementation was not properly handling the style injection order, causing SSR styles to be overridden by client-side styles during hydration.

## Solution
Replaced the custom `EmotionRegistry` implementation with the official `@mui/material-nextjs` package's `AppRouterCacheProvider`, which provides proper SSR support for Next.js App Router.

## Changes Made

### 1. Package Installation
- Installed `@mui/material-nextjs` package (already present in dependencies)

### 2. Updated `app/layout.tsx`
- **Removed**: Custom `EmotionRegistry` import and usage
- **Added**: `AppRouterCacheProvider` from `@mui/material-nextjs/v15-appRouter`
- **Key change**: Wrapped the app with `AppRouterCacheProvider` with `enableCssLayer: true` option
- **Import order**: Moved `globals.css` import after MUI imports to ensure proper CSS layer ordering

```tsx
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import "@/styles/globals.css"; // Moved after MUI imports

// In JSX:
<AppRouterCacheProvider options={{ enableCssLayer: true }}>
  <Providers>
    {/* app content */}
  </Providers>
</AppRouterCacheProvider>
```

### 3. Updated `lib/theme.ts`
- **Fixed**: `MuiAppBar` styleOverrides to use a function that properly accesses theme values
- **Before**: Used CSS variable `"var(--mui-palette-divider)"`
- **After**: Used theme callback `({ theme }) => ({ borderColor: theme.palette.divider })`

This ensures the theme values are properly resolved during SSR.

## Why This Works

1. **Proper Emotion Cache**: `AppRouterCacheProvider` creates an Emotion cache that's properly synchronized between server and client
2. **CSS Layers**: The `enableCssLayer: true` option ensures MUI styles are in the correct CSS layer, preventing specificity issues
3. **Correct Insertion Order**: The provider ensures SSR styles are inserted before client-side styles, maintaining the correct cascade
4. **Theme Resolution**: Using theme callbacks in styleOverrides ensures values are resolved correctly during both SSR and CSR

## Note on v15-appRouter vs v16-appRouter

The `@mui/material-nextjs` package currently provides up to `v15-appRouter`. This is fully compatible with Next.js 16, as the App Router architecture hasn't changed significantly. When/if a `v16-appRouter` export becomes available, you can update the import path.

## Files Modified
- `/app/layout.tsx` - Updated to use AppRouterCacheProvider
- `/lib/theme.ts` - Fixed MuiAppBar styleOverrides to use theme callback

## Files That Can Be Removed (Optional)
- `/components/providers/EmotionRegistry.tsx` - No longer used
- `/lib/createEmotionCache.ts` - No longer needed

## Testing
After these changes:
1. Start the dev server: `pnpm run dev`
2. Navigate to the app and check the navbar styling
3. Reload the page - styles should persist correctly
4. Toggle dark/light mode - styles should remain consistent
5. Check browser DevTools to ensure no style flash or hydration errors

## Additional Resources
- [MUI Next.js Integration Guide](https://mui.com/material-ui/guides/nextjs/)
- [MUI SSR Guide](https://mui.com/material-ui/guides/server-rendering/)
- [Next.js 16 App Router Docs](https://nextjs.org/docs/app)

