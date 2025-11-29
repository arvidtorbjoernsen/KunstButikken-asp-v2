# Architecture Audit Summary (2025-11-29)

## Overview
Branch: `chore/review-architecture`
Scope covers backend services (Admin, AuthGateway, Payment, User, Auction, Art) and the Next.js frontend. Focus areas: Dependency Injection, Clean Architecture, Repository pattern, DRY, and auth/security (frontend).

## Top Issues (Prioritized)
1. **Art/Auction services lack Clean Architecture layering**
   - *Impact*: Domain logic intertwined with EF infrastructure.
   - *Recommendation*: Introduce Application/Domain/Infrastructure projects mirroring Payment/User services.
   - *Effort*: 5d
   - *Dependencies*: Need shared DTO contracts + DI extensions.

2. **Repository interfaces missing in PaymentService**
   - *Impact*: DbContext leaks into Application layer, reducing testability.
   - *Recommendation*: Define repository interfaces in Application and inject Infrastructure implementations.
   - *Effort*: 2d

3. **Keycloak auth configuration duplicated across services**
   - *Impact*: Inconsistent audiences/authority; high maintenance.
   - *Recommendation*: Move Keycloak builder logic into `KunstButikken.ServiceDefaults` helper so ProgramSetup files call a single method.
   - *Effort*: 3d

4. **Frontend auth hardening (SSR + token refresh)**
   - *Impact*: Client-only token management; no SSR middleware; risk for future server actions.
   - *Recommendation*: Add middleware validating Keycloak session for SSR, ensure `apiFetch` refreshes tokens, document cookie strategy.
   - *Effort*: 2-3d

5. **Test coverage gaps for Auction/Art domain logic**
   - *Impact*: Critical flows untested.
   - *Recommendation*: After layer split, add targeted unit tests (e.g., bidding rules).
   - *Effort*: 2d bootstrap, ongoing.

## Supporting Documents
- Backend findings: `docs/audit/backend.md`
- Frontend findings: `docs/audit/frontend.md`

## Suggested Roadmap
1. **Week 1**: Plan Art/Auction layering + shared Keycloak helper design.
2. **Week 2**: Implement repository interfaces in Payment, start Art/Auction refactor.
3. **Week 3**: Frontend auth hardening + SSR middleware, begin new domain tests.
4. **Week 4**: Finish Art/Auction layering, add DI extensions, expand tests.

## Verification Steps
```bash
# Backend spot-checks
cd KunstButikken-asp
```

- `dotnet test KunstButikken.UserService/KunstButikken.UserService.Tests/KunstButikken.UserService.Tests.csproj`
- `dotnet test KunstButikken.PaymentService/KunstButikken.PaymentService.Tests/KunstButikken.PaymentService.Tests.csproj`

```bash
# Frontend tests
cd KunstButikken.Frontend
pnpm test
```

All above tests currently pass.

