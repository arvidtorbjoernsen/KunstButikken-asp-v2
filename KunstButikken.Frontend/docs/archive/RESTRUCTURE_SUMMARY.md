# Next.js Project Restructure Summary

## Date
November 15, 2025

## Overview
Reorganized the Next.js frontend project to follow proper Next.js conventions by moving source code into a `src/` directory.

## Changes Made

### 1. Directory Structure
**Before:**
```
KunstButikken.Frontend/
├── app/
├── components/
├── lib/
├── locales/
├── redux/
├── styles/
├── types/
├── public/
├── tests/
├── next.config.js
├── package.json
└── ...config files
```

**After:**
```
KunstButikken.Frontend/
├── src/
│   ├── app/              # Next.js App Router
│   ├── components/       # React components
│   ├── lib/              # Utility functions and API clients
│   ├── locales/          # Internationalization files
│   ├── redux/            # Redux store and slices
│   ├── styles/           # Global styles
│   └── types/            # TypeScript type definitions
├── public/               # Static assets (unchanged)
├── tests/                # Test files (unchanged)
├── next.config.js
├── package.json
└── ...config files
```

### 2. Configuration Updates

#### tsconfig.json
- Changed `baseUrl` from `"."` to `"src"`
- Path aliases (`@/*`) now resolve relative to `src/`

#### jest.config.cjs
- Updated `moduleNameMapper` for `@/` alias:
  - Before: `'^@/(.*)$': '<rootDir>/$1'`
  - After: `'^@/(.*)$': '<rootDir>/src/$1'`

#### tailwind.config.js
- Updated content paths to scan `src/` directory:
  - Before: `'./app/**/*.{js,ts,jsx,tsx}'`
  - After: `'./src/app/**/*.{js,ts,jsx,tsx}'`
  - (Same for components and pages)

### 3. Files That Remained at Root Level
- `public/` - Static assets must remain at root for Next.js
- `tests/` - Test files (e2e, unit tests)
- `scripts/` - Build and utility scripts
- Configuration files:
  - `next.config.js`
  - `package.json`
  - `tsconfig.json`
  - `jest.config.cjs`
  - `playwright.config.ts`
  - `eslint.config.mjs`
  - `tailwind.config.js`
  - `postcss.config.js`
  - `.env.*` files

### 4. Import Resolution
All existing imports using the `@/` alias continue to work correctly because:
1. TypeScript resolves `@/` relative to `src/` (via `tsconfig.json`)
2. Jest resolves `@/` to `<rootDir>/src/` (via `jest.config.cjs`)
3. Next.js automatically supports the `src/` directory

## Benefits

1. **Clearer Project Structure**: Separation of source code from configuration
2. **Industry Standard**: Follows Next.js recommended practices
3. **Better Organization**: Source files grouped under `src/`
4. **Improved Maintainability**: Easier to navigate and understand project layout
5. **Zero Breaking Changes**: All imports and builds work without code changes

## Verification

✅ **Build**: Production build successful (`pnpm build`)
✅ **TypeScript**: No compilation errors
✅ **Module Resolution**: All `@/` imports resolve correctly
✅ **Configuration**: All config files updated and validated

## Next Steps (Optional)

Consider these additional improvements:
1. Move test files into `src/` alongside components (co-located tests)
2. Add `src/middleware.ts` for Next.js middleware if needed
3. Consider adding `src/hooks/` for custom React hooks
4. Add `src/utils/` or `src/helpers/` if needed

## References

- [Next.js src Directory Documentation](https://nextjs.org/docs/app/building-your-application/configuring/src-directory)
- [Next.js Project Structure Best Practices](https://nextjs.org/docs/getting-started/project-structure)

