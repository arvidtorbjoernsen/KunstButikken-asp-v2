# Next.js Server-Side Rendering (SSR) Implementation

This document describes how the Next.js frontend has been configured for Server-Side Rendering (SSR), similar to the Angular application's SSR setup.

## Overview

The Next.js app now uses SSR by default for all pages, providing:
- **Better SEO**: Search engines receive fully rendered HTML
- **Faster initial page load**: Server sends rendered HTML instead of empty shell
- **Improved performance**: Data fetching happens on the server before rendering
- **Better UX**: Users see content faster, similar to the Angular SSR experience

## Architecture

### Server Components vs Client Components

Following Next.js 16 and React 19 best practices, the app uses:

1. **Server Components (Default)**
   - Pages without `"use client"` directive
   - Fetch data on the server
   - Generate HTML before sending to client
   - Example: `app/page.tsx`, `app/art/page.tsx`

2. **Client Components**
   - Components with `"use client"` directive
   - Handle interactivity (clicks, state, hooks)
   - Hydrate on the client after server rendering
   - Example: `app/HomePageClient.tsx`, `components/nav/Navbar.tsx`

### Pattern: Server Page → Client Component

Most pages follow this pattern (similar to Angular's resolver + component):

```typescript
// app/page.tsx (Server Component)
async function getData() {
  // Fetch on server
  const res = await fetch('api/endpoint', { next: { revalidate: 60 } });
  return res.json();
}

export default async function Page() {
  const data = await getData();
  return <ClientComponent data={data} />;
}

// ClientComponent.tsx
"use client";
export default function ClientComponent({ data }) {
  // Handle interactivity
  return <div>...</div>;
}
```

## Configuration

### next.config.js

```javascript
const nextConfig = {
  reactStrictMode: true,
  experimental: {
    serverActions: {
      allowedOrigins: ["localhost:3000"],
    },
  },
  images: {
    remotePatterns: [
      { protocol: 'http', hostname: 'localhost' },
      { protocol: 'http', hostname: '127.0.0.1' },
    ],
  },
};
```

### Environment Configuration

Created `lib/environment.ts` for centralized config (similar to Angular's `environment.ts`):

```typescript
// Server-side only
export function getServerEnvironment(): Environment { ... }

// Client-side safe subset
export function getClientEnvironment(): Partial<Environment> { ... }
```

## SSR-Enabled Pages

### Home Page (`app/page.tsx`)
- **Server Component**: Fetches featured art
- **Client Component**: `HomePageClient` for interactivity
- **ISR**: Revalidates every 60 seconds

### Art Listing (`app/art/page.tsx`)
- **Server Component**: Fetches all art + featured
- **Client Component**: `ArtForSaleClient` for filtering/display
- **ISR**: Revalidates every 60 seconds

### Art Detail (`app/art/[id]/page.tsx`)
- **Server Component**: Fetches single art item
- **Client Component**: `ArtDetailClient` for display
- **Dynamic Route**: Generates pages for each art ID
- **ISR**: Revalidates every 60 seconds

### Auctions (`app/art/auctions/page.tsx`)
- **Server Component**: Page wrapper
- **Client Component**: `AuctionsClient` (placeholder for future)

## Provider Structure

Refactored providers for better SSR support and authentication:

```
app/layout.tsx (Server Component)
└── Providers (Client Component)
    ├── KeycloakProvider (Keycloak auth)
    ├── SessionProvider (NextAuth)
    ├── ThemeProvider (MUI + Dark mode)
    │   └── CssBaseline
    ├── TranslationProvider (i18n)
    └── ReduxProvider (State management)
```

Each provider is client-side only where needed, but receives initial data from server. The `KeycloakProvider` is the outermost provider to ensure authentication is available to all child components.

## Data Fetching Strategies

### 1. Server-Side Rendering (SSR)
```typescript
// Fetches on every request
export default async function Page() {
  const data = await fetch('api/endpoint');
  return <Client data={data} />;
}
```

### 2. Incremental Static Regeneration (ISR)
```typescript
// Cached, revalidated every N seconds
const res = await fetch('api/endpoint', {
  next: { revalidate: 60 }
});
```

### 3. Static Generation (SSG)
```typescript
// Generated at build time
const res = await fetch('api/endpoint', {
  cache: 'force-cache'
});
```

## Comparison with Angular SSR

| Feature | Angular | Next.js |
|---------|---------|---------|
| SSR Setup | `server.ts` + `@angular/ssr` | Built-in (default) |
| Data Fetching | Resolvers + Interceptors | `async` Server Components |
| Client Hydration | `bootstrapApplication()` | Automatic |
| Environment Config | `environment.ts` | `lib/environment.ts` |
| Route Config | `routes.ts` | File-based routing |
| State Management | Services + Signals | React Context + Hooks |

## Authentication with SSR

The app uses a hybrid authentication approach:

### Keycloak (Primary)
- **Client-Side**: Direct Keycloak integration via `keycloak-js`
- **SSO**: Silent check-sso for seamless authentication
- **API Auth**: Automatic token attachment via `api-client.ts`
- **Similar to**: Angular's KeycloakService implementation
- **See**: [KEYCLOAK_MIGRATION.md](./KEYCLOAK_MIGRATION.md) for details

### NextAuth (Secondary)
- **Server**: Session accessed via `auth()` in Server Components
- **Client**: Session accessed via `useSession()` hook
- **Database Sessions**: Stored in PostgreSQL via Prisma (prevents cookie overflow)

## Performance Benefits

1. **Time to First Byte (TTFB)**: Server sends HTML immediately
2. **First Contentful Paint (FCP)**: Users see content faster
3. **SEO**: Search engines index full content
4. **Caching**: ISR provides static-like performance with dynamic data

## Development vs Production

### Development
- SSR enabled by default
- Fast refresh for instant updates
- Dev server on port 3000

### Production
- Build: `npm run build`
- Start: `npm start`
- Optimized bundles with automatic code splitting

## Best Practices

1. **Keep Server Components Server**
   - Don't add `"use client"` unless needed
   - Let Next.js optimize server rendering

2. **Minimize Client Components**
   - Only use for interactivity
   - Pass data from Server Components

3. **Use ISR for Dynamic Content**
   - `{ next: { revalidate: 60 } }`
   - Balance freshness vs performance

4. **Handle Loading States**
   - Server Components don't show loading
   - Use Suspense boundaries if needed

5. **Environment Variables**
   - `NEXT_PUBLIC_*` for client-side
   - No prefix for server-side only

## Troubleshooting

### "use client" needed?
If you see errors about hooks or browser APIs, add `"use client"` to the component.

### Data not refreshing?
Check ISR revalidation time or use `{ cache: 'no-store' }` for always-fresh data.

### Hydration mismatch?
Ensure server and client render the same initial HTML. Use `suppressHydrationWarning` sparingly.

## Future Enhancements

- [ ] Add `loading.tsx` for Suspense boundaries
- [ ] Implement `error.tsx` for error handling
- [ ] Add metadata generation for dynamic pages
- [ ] Consider streaming SSR for large pages
- [ ] Add service worker for offline support
