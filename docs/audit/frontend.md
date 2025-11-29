# Frontend (Next.js) Architecture Audit (2025-11-29)

## Scope
Examined `KunstButikken.Frontend` with focus on tsyringe DI usage, Clean Architecture boundaries, auth & security practices, and DRY adherence.

## Summary Table
| Area | Status | Priority | Effort |
| --- | --- | --- | --- |
| tsyringe DI for infrastructure/services | ✅ | - | - |
| Clean Architecture layers (application/useCases, infrastructure, features) | ✅ with minor leaks | Low | 1d |
| Auth & security (Keycloak provider, token handling) | ⚠️ Needs SSR token handling, token refresh tests | Medium | 2d |
| DRY (shared config, API client) | ✅ | - | - |
| Testing coverage (unit/integration) | ✅ (74 tests) | - | - |

## Detailed Findings

### 1. Dependency Injection via tsyringe
- Infrastructure container (`src/infrastructure/di/container.ts`) registers repositories and API client. Usage patterns (useCases) resolve via DI tokens.
- **Improvement**: add scoped lifetime handling for per-request state (currently singleton). Consider `container.createChildContainer()` in React contexts.
- **Effort**: Low (0.5d).

### 2. Clean Architecture layering
- Application use cases under `src/application/useCases` keep business logic separate from UI; repositories implement interfaces.
- Direct infrastructure imports (e.g., raw `apiClient` usage) are no longer present outside repository/tests; DI boundaries hold.
- **Action**: keep a lint/checklist to prevent future feature modules from importing infrastructure factories directly. No code change needed today.
- **Effort**: Low (add lint rule/doc in 0.5d if desired).

### 3. Auth & Security
- Keycloak provider handles token attach + refresh, but SSR routes (app directory server components) rely on client token only.
- Missing CSRF enforcement for POSTs routed via AuthGateway; no SameSite settings.
- **Recommendations**:
  1. Add Edge/server middleware to validate Keycloak session for RSC/SSR (1d).
  2. Extend `apiFetch` to auto-refresh tokens when Keycloak `updateToken` resolves (currently only logs) (1d).
  3. Document secure cookie/SameSite strategy for future server actions (0.5d).

### 4. DRY
- Shared config via `src/shared/config.ts` prevents duplication; `api-client` centralizes fetch logic. No immediate issues.

## Next Steps / Owners
1. Auth hardening tasks (frontend platform dev, 2d).
2. Feature cleanup to ensure infra access only via DI/use cases (frontend dev, 1d).
3. Add SSR auth middleware sample and docs (frontend lead, 1d).
