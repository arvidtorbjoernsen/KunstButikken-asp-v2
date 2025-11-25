# Keycloak Authentication Migration

This document describes the migration from NextAuth-only authentication to a hybrid approach using Keycloak directly (similar to the Angular app) while maintaining NextAuth for session management.

## Overview

The Next.js app now uses Keycloak authentication similar to the Angular application, providing:
- **Direct Keycloak Integration**: Client-side keycloak-js library
- **SSO Support**: Silent check-sso for seamless authentication
- **Token Management**: Automatic token refresh
- **API Authentication**: Automatic token attachment to API requests
- **Consistent UX**: Same authentication flow as Angular app

## Architecture

### Keycloak Provider

Similar to Angular's `KeycloakService` initialized via `APP_INITIALIZER`, the Next.js app uses:

```typescript
// lib/keycloak.tsx
<KeycloakProvider>
  {children}
</KeycloakProvider>
```

This provider:
1. Initializes Keycloak on client mount
2. Uses `check-sso` for silent authentication
3. Manages authentication state via React context
4. Handles token refresh automatically
5. Provides authentication methods (login, logout, getToken)

### API Client with Interceptor

Similar to Angular's `authTokenInterceptor`, we have:

```typescript
// lib/api-client.ts
export function apiFetch(url: string, options?: RequestInit): Promise<Response>
export const apiClient = { get, post, put, delete, patch }
```

This automatically:
- Attaches Keycloak token to API requests
- Only applies to requests going to the API gateway
- Logs token attachment in development mode

## Comparison with Angular Implementation

| Feature | Angular | Next.js |
|---------|---------|---------|
| Keycloak Init | `APP_INITIALIZER` + `KeycloakService.boot()` | `KeycloakProvider` + `useEffect` |
| Authentication State | `signal<boolean>` | React `useState` + Context |
| Token Attachment | `authTokenInterceptor` (HTTP interceptor) | `apiFetch` utility function |
| SSO Check | `check-sso` + `silent-check-sso.html` | Same |
| Token Refresh | `onTokenExpired` + `updateToken(30)` | Same |
| Login/Logout | `keycloak.login()` / `keycloak.logout()` | Same |

## Configuration

### Environment Variables

Similar to Angular's `environment.ts`, configure via env vars:

```bash
NEXT_PUBLIC_KEYCLOAK_BASE_URL=http://localhost:8080
NEXT_PUBLIC_KEYCLOAK_REALM=kunstbutikken
NEXT_PUBLIC_KEYCLOAK_CLIENT_ID=kunstbutikken-frontend
NEXT_PUBLIC_FRONTEND_URL=http://localhost:3000
NEXT_PUBLIC_API_GATEWAY=http://localhost:5000
```

### Silent SSO

The `public/silent-check-sso.html` file enables silent SSO checks:

```html
<!DOCTYPE html>
<html>
<body>
<script>
  parent.postMessage(location.href, location.origin);
</script>
</body>
</html>
```

This is identical to the Angular app's implementation.

## Components

### New Components

1. **`lib/keycloak.tsx`**
   - Keycloak provider and React hooks
   - Replaces NextAuth-only approach
   - Similar to Angular's `KeycloakService`

2. **`lib/api-client.ts`**
   - Authenticated fetch utilities
   - Auto token attachment
   - Similar to Angular's HTTP interceptor

3. **`components/nav/NavbarKeycloak.tsx`**
   - Navbar using Keycloak auth
   - Client-side component
   - Similar to Angular's navbar

4. **`components/nav/NavbarAuthActionsKeycloak.tsx`**
   - Auth actions (login/logout) using Keycloak
   - Replaces NextAuth-based actions

5. **`components/nav/MobileMenuKeycloak.tsx`**
   - Mobile menu using Keycloak
   - Client-side auth state

### Updated Components

1. **`components/providers/Providers.tsx`**
   - Added `KeycloakProvider` wrapper
   - Now provides both Keycloak and NextAuth contexts

2. **`app/layout.tsx`**
   - Uses `NavbarKeycloak` instead of `Navbar`
   - Keycloak-based authentication in navbar

## Usage

### Using Keycloak in Components

```typescript
"use client";

import { useKeycloak } from "@/lib/keycloak";

function MyComponent() {
  const { authenticated, loading, login, logout, getToken } = useKeycloak();

  if (loading) return <div>Loading...</div>;

  if (!authenticated) {
    return <button onClick={login}>Sign In</button>;
  }

  return <button onClick={logout}>Sign Out</button>;
}
```

### Making Authenticated API Calls

```typescript
import { apiClient } from "@/lib/api-client";

// Token is automatically attached if going to API gateway
const data = await apiClient.get("http://localhost:5000/api/art");
const result = await apiClient.post("http://localhost:5000/api/art", { title: "New Art" });
```

### Direct Fetch with Auth

```typescript
import { apiFetch } from "@/lib/api-client";

const response = await apiFetch("http://localhost:5000/api/art", {
  method: "GET"
});
```

## Authentication Flow

### Initial Load (SSO Check)

1. User loads app
2. `KeycloakProvider` initializes
3. Keycloak performs `check-sso` via iframe
4. If SSO session exists, user is authenticated
5. Navbar updates to show authenticated state

Similar to Angular's initialization in `app.config.ts` with `APP_INITIALIZER`.

### Login Flow

1. User clicks "Sign In"
2. `login()` redirects to Keycloak
3. User authenticates on Keycloak
4. Redirects back to app
5. Keycloak initializes with token
6. App shows authenticated state

### Token Refresh

1. Token expires (detected by Keycloak)
2. `onTokenExpired` callback fires
3. Calls `updateToken(30)` to refresh
4. If refresh succeeds, continues
5. If refresh fails, logs user out

Identical to Angular's token refresh logic.

### Logout Flow

1. User clicks "Log Out"
2. `logout()` called
3. Redirects to Keycloak logout
4. Keycloak terminates session
5. Redirects back to app
6. App shows unauthenticated state

## Hybrid Approach

The app uses both Keycloak and NextAuth:

### Keycloak (Client-Side)
- **Purpose**: Direct SSO integration, token management
- **Usage**: Navbar, API calls, authentication state
- **Similar to**: Angular's KeycloakService

### NextAuth (Server-Side)
- **Purpose**: Session persistence, server-side auth
- **Usage**: API routes, server components (where needed)
- **Advantage**: Database-backed sessions

This hybrid approach provides:
- Best of both worlds
- Client-side SSO (like Angular)
- Server-side session management (Next.js advantage)

## Benefits

### Consistency with Angular
✅ Same Keycloak configuration
✅ Same SSO flow
✅ Same token management
✅ Compatible API authentication

### Next.js Advantages
✅ React hooks for auth state
✅ Context API for global state
✅ Server-side session fallback
✅ Type-safe API client

## Migration Checklist

- ✅ Install `keycloak-js` package
- ✅ Create `KeycloakProvider` component
- ✅ Create API client with auto auth
- ✅ Update Navbar to use Keycloak
- ✅ Add environment configuration
- ✅ Test SSO flow
- ✅ Test token refresh
- ✅ Test login/logout

## Testing

### Test SSO

1. Login to Angular app
2. Open Next.js app in same browser
3. Should be automatically authenticated

### Test Token Refresh

1. Login to app
2. Wait for token to expire (check console)
3. Make API request
4. Token should refresh automatically

### Test Logout

1. Login to app
2. Click logout
3. Should redirect to Keycloak
4. Should return logged out

## Troubleshooting

### "Keycloak not initialized"

Check environment variables are set correctly:
- `NEXT_PUBLIC_KEYCLOAK_BASE_URL`
- `NEXT_PUBLIC_KEYCLOAK_REALM`
- `NEXT_PUBLIC_KEYCLOAK_CLIENT_ID`

### "No token available"

User might not be authenticated:
- Check `authenticated` state
- Call `login()` to authenticate

### "Token refresh failed"

Refresh token might be expired:
- User needs to login again
- Check Keycloak session timeout settings

## Next Steps

1. Consider removing NextAuth if Keycloak is sufficient
2. Add role-based access control using Keycloak roles
3. Implement token introspection for server-side validation
4. Add Keycloak account management integration
