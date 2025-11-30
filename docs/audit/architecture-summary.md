# Architecture Audit Summary (2025-11-30)

## Overview
Branch: `chore/review-architecture`
Scope covers backend services (Admin, AuthGateway, Payment, User, Auction, Art) and the Next.js frontend. Focus areas: Dependency Injection, Clean Architecture, Repository pattern, DRY, and auth/security (frontend).

## Top Issues (Prioritized)
1. **Auction/Art services still leaking Infrastructure into controllers**
   - *Impact*: SignalR hubs + controllers talk to DbContexts directly despite Application/Domain split, limiting testability and violating Clean Architecture.
   - *Recommendation*: Route HTTP/Hub flows through Application handlers + repositories; introduce MediatR or command/query services.
   - *Effort*: 5-7d
   - *Dependencies*: Requires DI extension updates + refactoring ProgramSetup auth wiring.

2. **PaymentService handlers bypass repositories**
   - *Impact*: Recent payout/settlement commands inject `PaymentDbContext`, making unit tests cumbersome.
   - *Recommendation*: Define `IPayoutRepository`/`ISettlementRepository` in Application layer, adapt Infrastructure implementation.
   - *Effort*: 2-3d

3. **Keycloak auth configuration duplicated across services**
   - *Impact*: Each ProgramSetup parses `KEYCLOAK_*`; inconsistencies risk auth failures.
   - *Recommendation*: Build `ServiceDefaults.AddKeycloakAuth(...)` helper and adopt across services.
   - *Effort*: 3d

4. **Frontend auth hardening (SSR + token telemetry)**
   - *Impact*: Middleware covers only defined matchers; server actions/RSC still rely on client tokens, no telemetry for refresh failures.
   - *Recommendation*: Add server-side auth helpers, propagate middleware verdict via headers, send refresh failures to Sentry, finalize cookie contract.
   - *Effort*: 2-3d

5. **Test coverage gaps for Art/Auction domains**
   - *Impact*: Auction only 2 unit tests; Art still sample tests; regressions likely.
   - *Recommendation*: Add domain service tests + integration-event contract tests once layers enforced.
   - *Effort*: 4d bootstrap, ongoing.

## Supporting Documents
- Backend findings: `docs/audit/backend.md`
- Frontend findings: `docs/audit/frontend.md`

## Suggested Roadmap
1. **Week 1**: Implement shared Keycloak helper; plan controller refactors for Auction/Art.
2. **Week 2**: Refactor Payment handlers to repositories; begin Auction/Art handler rewrites.
3. **Week 3**: Frontend auth utilities + telemetry, expand middleware coverage, add SSR tests.
4. **Week 4**: Finish domain refactors, add DI extensions + domain tests for Art/Auction.

## Verification Steps
```bash
# Backend spot-checks
cd KunstButikken-asp

dotnet test KunstButikken.PaymentService/KunstButikken.PaymentService.Tests/KunstButikken.PaymentService.Tests.csproj
dotnet test KunstButikken.UserService/KunstButikken.UserService.Tests/KunstButikken.UserService.Tests.csproj
dotnet test KunstButikken.AuctionService/KunstButikken.AuctionService.Tests/KunstButikken.AuctionService.Tests.csproj
```
```bash
# Frontend tests
cd KunstButikken.Frontend
pnpm install --frozen-lockfile
pnpm test
```
- Backend suites above pass (Payment: 5 tests, User: 1, Auction: 2).
- Frontend: 24 suites / 79 tests, ~80% statement coverage.
