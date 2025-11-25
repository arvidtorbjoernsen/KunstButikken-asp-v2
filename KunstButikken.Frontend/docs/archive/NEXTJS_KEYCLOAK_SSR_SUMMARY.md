# Next.js SSR + Keycloak Integration - Complete Summary

This document provides a quick overview of the SSR and Keycloak authentication migration completed for the Next.js frontend.

## What Was Done

### 1. Server-Side Rendering (SSR)
✅ Configured Next.js for full SSR (like Angular)
✅ Created Server Component / Client Component split pattern
✅ Implemented ISR (Incremental Static Regeneration) for art pages
✅ Added centralized environment configuration (`lib/environment.ts`)
✅ Refactored providers for SSR compatibility

**See**: [SSR_IMPLEMENTATION.md](./SSR_IMPLEMENTATION.md) for full details

### 2. Keycloak Authentication
✅ Added `keycloak-js` package
✅ Created `KeycloakProvider` with React context (like Angular's KeycloakService)
✅ Created API client with automatic token injection (like Angular's HTTP interceptor)
✅ Built Keycloak-based navbar components
✅ Integrated into app layout

**See**: [KEYCLOAK_MIGRATION.md](./KEYCLOAK_MIGRATION.md) for full details

## Key Files Created/Modified

### New Files
- `lib/keycloak.tsx` - Keycloak provider and hooks
- `lib/api-client.ts` - Authenticated API client
- `lib/environment.ts` - Environment configuration
- `components/providers/ThemeProvider.tsx` - Extracted theme provider
- `components/nav/NavbarKeycloak.tsx` - Keycloak navbar
- `components/nav/NavbarAuthActionsKeycloak.tsx` - Auth actions
- `components/nav/MobileMenuKeycloak.tsx` - Mobile menu
- `app/HomePageClient.tsx` - Client component for home
- `app/art/ArtForSaleClient.tsx` - Client component for art listing
- `app/art/ArtDetailClient.tsx` - Client component for art detail

### Modified Files
- `next.config.js` - SSR configuration
- `package.json` - Added keycloak-js, updated scripts
- `components/providers/Providers.tsx` - Added KeycloakProvider
- `app/layout.tsx` - Uses NavbarKeycloak
- `app/page.tsx` - Server Component pattern
- `app/art/page.tsx` - Server Component pattern
- `app/art/[id]/page.tsx` - Server Component pattern

## Architecture

```
Next.js App (SSR)
├── Server Components (Data Fetching)
│   ├── app/page.tsx
│   ├── app/art/page.tsx
│   └── app/art/[id]/page.tsx
│
├── Client Components (Interactivity)
│   ├── HomePageClient.tsx
│   ├── ArtForSaleClient.tsx
│   └── ArtDetailClient.tsx
│
└── Providers (Authentication & State)
    ├── KeycloakProvider (Keycloak auth)
    ├── SessionProvider (NextAuth)
    ├── ThemeProvider (MUI theming)
    ├── TranslationProvider (i18n)
    └── ReduxProvider (State)
```

## How It Works

### SSR Flow
1. User requests page
2. Server Component fetches data
3. Server renders HTML with data
4. Client receives rendered HTML
5. Client Component hydrates for interactivity

### Keycloak Flow
1. App loads → `KeycloakProvider` initializes
2. Performs silent SSO check
3. If session exists → user authenticated
4. API calls → token automatically attached
5. Token expires → automatic refresh

## Next Steps

1. **Install Dependencies**
   ```bash
   cd KunstButikken.Frontend
   pnpm install
   ```

2. **Test SSR**
   ```bash
   pnpm dev
   # Visit http://localhost:3000
   # Check page source - should see rendered HTML
   ```

3. **Test Keycloak**
   - Click "Sign In" → should redirect to Keycloak
   - Login → should return authenticated
   - Check console for token attachment logs
   - Wait for token expiry → should refresh automatically

4. **Optional: Update Other Components**
   - Replace `useSession()` with `useKeycloak()` where appropriate
   - Use `apiClient` for authenticated API calls
   - Maintain NextAuth for server-side needs

## Documentation

- [SSR_IMPLEMENTATION.md](./SSR_IMPLEMENTATION.md) - Complete SSR guide
- [KEYCLOAK_MIGRATION.md](./KEYCLOAK_MIGRATION.md) - Complete Keycloak guide
- [NEXTJS_SSR_MIGRATION.md](./NEXTJS_SSR_MIGRATION.md) - Migration checklist

## Benefits

### SSR Benefits
- ✅ Better SEO (search engines see full HTML)
- ✅ Faster initial page load
- ✅ Improved Core Web Vitals
- ✅ Consistent with Angular architecture

### Keycloak Benefits
- ✅ SSO with Angular app
- ✅ Consistent authentication flow
- ✅ Automatic token management
- ✅ Silent authentication check

## Comparison with Angular

| Feature | Angular | Next.js |
|---------|---------|---------|
| SSR | `@angular/ssr` | Built-in (default) |
| Keycloak Init | `APP_INITIALIZER` | `KeycloakProvider` |
| Token Interceptor | `authTokenInterceptor` | `api-client.ts` |
| Auth Service | `KeycloakService` | `useKeycloak()` hook |
| Silent SSO | `check-sso` + iframe | Same |
| Token Refresh | `updateToken(30)` | Same |

## Troubleshooting

### Dependencies Not Installed
```bash
cd KunstButikken.Frontend
pnpm install
```

### Keycloak Not Initializing
Check environment variables in `.env.local`:
```env
NEXT_PUBLIC_KEYCLOAK_BASE_URL=http://localhost:8080
NEXT_PUBLIC_KEYCLOAK_REALM=kunstbutikken
NEXT_PUBLIC_KEYCLOAK_CLIENT_ID=kunstbutikken-frontend
```

### SSR Hydration Errors
- Ensure server/client render same HTML
- Check for browser-only code in Server Components
- Use `suppressHydrationWarning` only when necessary

### Token Not Attached
- Check API URL matches `NEXT_PUBLIC_API_GATEWAY`
- Verify user is authenticated (`authenticated === true`)
- Check console for token attachment logs

## Questions?

- **SSR Issues**: See [SSR_IMPLEMENTATION.md](./SSR_IMPLEMENTATION.md)
- **Keycloak Issues**: See [KEYCLOAK_MIGRATION.md](./KEYCLOAK_MIGRATION.md)
- **Migration Checklist**: See [NEXTJS_SSR_MIGRATION.md](./NEXTJS_SSR_MIGRATION.md)
