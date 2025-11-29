# Frontend Auth Hardening Notes

## Middleware
- `src/middleware.ts` now guards `/auth/me`, `/profile`, `/admin/**`, and `/seller/**`.
- Treats authentication as valid when any of `KEYCLOAK_SESSION`, `AUTHGATEWAY_SESSION`, or `AUTHGATEWAY_REFRESH` cookies are present. `AUTHGATEWAY_*` cookies are expected to be Secure/HttpOnly once the gateway issues them.
- Admin routes trigger a lightweight `POST /auth/session/validate` call (expects AuthGateway support) to ensure the session has server-side privileges.
- Redirects preserve an allow-listed `redirect` query param when provided, preventing open redirect issues.

## Token Refresh
- Client-side `api-client` automatically retries once after 401/403 by invoking `Keycloak.updateToken`. Jest tests cover both success and failure scenarios.

## Cookies & CSRF
- Plan: AuthGateway issues two HttpOnly cookies
  - `AUTHGATEWAY_SESSION`: signed session id (`Secure`, `SameSite=Lax`).
  - `AUTHGATEWAY_REFRESH`: refresh token or opaque handle (`Secure`, `SameSite=Strict`).
- Keycloak cookie (`KEYCLOAK_SESSION`) remains optional for compatibility but should be non-HttpOnly.
- Once cookies ship, document server-origin hardening (e.g., CSRF double-submit tokens) and update middleware to clear stale cookies when redirecting to `/auth/logout`.
