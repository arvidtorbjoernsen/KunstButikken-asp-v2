# Frontend Auth Hardening Notes

## Middleware
- `src/middleware.ts` now guards `/auth/me`, `/profile`, `/admin/**`, and `/seller/**`.
- Treats authentication as valid when any of `KEYCLOAK_SESSION`, `AUTHGATEWAY_SESSION`, or `AUTHGATEWAY_REFRESH` cookies are present. `AUTHGATEWAY_*` cookies are expected to be Secure/HttpOnly once the gateway issues them.
- Admin routes trigger a lightweight `GET /auth/session/validate` call (credentials included) so the AuthGateway can confirm the session cookie server-side.
- Redirects preserve an allow-listed `redirect` query param when provided, preventing open redirect issues.

## Login flow (Next.js)
1. `SignInClient` triggers the regular Keycloak login if the user is unauthenticated and no `code` query parameter is present.
2. After Keycloak redirects back to `/auth/signin?code=…&callbackUrl=…`, `SignInClient` calls `exchangeCodeForGatewaySession` (`/src/features/auth/lib/gateway-session.ts`).
3. The helper POSTs `{ code, redirectUri }` to `${NEXT_PUBLIC_API_GATEWAY}/auth/session` with `credentials: 'include'`. AuthGateway exchanges the code with Keycloak, mints `AUTHGATEWAY_SESSION` + `AUTHGATEWAY_REFRESH`, and the browser stores them as HttpOnly cookies.
4. On success the client `router.replace()`'s the cleaned `callbackUrl` (defaults to `/`). On failure it routes to `/auth/error?reason=session`.
5. Subsequent protected-route navigation relies on cookies only; the Keycloak adapter remains for token utilities until we remove bearer headers entirely.

### QA checklist
- **Keycloak redirect:** Hit `/auth/signin?callbackUrl=/profile`, confirm you are redirected to Keycloak and returned with `code`.
- **Cookie issuance:** Inspect the browser storage after the code exchange; `AUTHGATEWAY_SESSION` should be Secure/HttpOnly with `SameSite=Lax`, refresh Strict.
- **Middleware:** Visiting `/admin` without cookies redirects to `/auth/signin`. With cookies but revoked session, middleware hits `/auth/session/validate` and redirects to `/auth/forbidden`.
- **Logout:** `/auth/session/logout` clears cookies; middleware should then force a re-login on the next protected navigation.

## Token Refresh
- Client-side `api-client` automatically retries once after 401/403 by invoking `Keycloak.updateToken`. Jest tests cover both success and failure scenarios.

## Cookies & CSRF
- AuthGateway issues two HttpOnly cookies
  - `AUTHGATEWAY_SESSION`: signed session id (`Secure`, `SameSite=Lax`).
  - `AUTHGATEWAY_REFRESH`: refresh token or opaque handle (`Secure`, `SameSite=Strict`).
- Keycloak cookie (`KEYCLOAK_SESSION`) remains optional for compatibility but should be non-HttpOnly.
- When the gateway ships CSRF tokens we can forward them via `NextResponse` headers and double-submit tokens for mutations.
