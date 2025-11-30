# Frontend (Next.js) Architecture Audit (2025-11-30)

## Scope
Examined `KunstButikken.Frontend` with focus on tsyringe DI usage, Clean Architecture boundaries, auth & security practices, and DRY adherence.

## Summary Table
| Area | Status | Priority | Effort |
| --- | --- | --- | --- |
| tsyringe DI for infrastructure/services | ✅ | - | - |
| Clean Architecture layers (application/useCases, infrastructure, features) | ✅ with minor leaks | Low | 1d |
| Auth & security (Keycloak provider, token handling) | ⚠️ SSR middleware in place but server actions lack token checks | Medium | 2-3d |
| Testing coverage (unit/integration) | ✅ (79 tests, ~80% stmts) | - | - |

## Detailed Findings

### 1. Dependency Injection via tsyringe
- Infrastructure container (`src/infrastructure/di/container.ts`) registers repositories and API client. Usage patterns (useCases) resolve via DI tokens.
- **Improvement**: add scoped lifetime handling for per-request state (currently singleton). Consider `container.createChildContainer()` in React contexts.
- **Effort**: Low (0.5d).
- **Update**: `createRequestScope()` landed but app router entrypoints still resolve the global container; server actions should request a child scope per request to avoid leaked tokens.

### 2. Clean Architecture layering
- Application use cases under `src/application/useCases` keep business logic separate from UI; repositories implement interfaces.
- Direct infrastructure imports (e.g., raw `apiClient` usage) are no longer present outside repository/tests; DI boundaries hold.
- **Action**: keep a lint/checklist to prevent future feature modules from importing infrastructure factories directly. No code change needed today.
- **Effort**: Low (add lint rule/doc in 0.5d if desired).

### 3. Auth & Security
- **Progress**: Edge middleware in `src/middleware.ts` now enforces Keycloak/AuthGateway cookies for `/auth`, `/profile`, `/admin`, `/seller` routes and validates sessions against the gateway.
- **Remaining gaps**:
  1. Server Actions / RSC handlers bypass the middleware and still rely on client-only tokens.
  2. `apiFetch` refresh flow exists (see tests) but production env lacks metrics/alerts when refresh fails; extend logging to Sentry.
  3. Cookies are marked `SameSite=Lax` by default; admin/seller flows need `Secure` + `SameSite=None` coordination with AuthGateway to avoid silent drops in Chromium.
- **Recommendation**: add shared auth utility for server components, wire middleware verdict into `headers()` for SSR, and document cookie contract in `docs/frontend/auth.md`.

### 4. DRY
- **Note**: Config + API client remain centralized; consider extracting middleware patterns into `src/shared/auth` to reuse between API routes and middleware.

### 5. Testing coverage
- **Result**: `pnpm test` runs 24 suites / 79 specs (~80% statements, see latest coverage report).
- **Action**: add server-action auth tests once middleware contract is enforced; add playwright SSR smoke once new flows exist.

## Next Steps / Owners
1. Adopt per-request DI scopes in app router/server actions (frontend dev, 1d).
2. Extend auth middleware + server utilities to cover SSR/server actions (frontend platform, 2d).
3. Document cookie strategy + add telemetry for token refresh failures (frontend lead, 1d).
4. Add auth regression tests (unit + Playwright) covering middleware redirects (QA/dev, 2d).
