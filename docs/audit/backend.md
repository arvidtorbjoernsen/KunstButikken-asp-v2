# Backend Architecture Audit (2025-11-30)

## Scope
Services reviewed: AdminService, AuthGateway, PaymentService, UserService, AuctionService, ArtService. Focus areas: Dependency Injection (DI), Clean Architecture adherence, Repository Pattern usage, DRY compliance.

## Summary Table
| Area | Status | Priority | Effort |
| --- | --- | --- | --- |
| DI registrations centralized per service | ✅ | - | - |
| Clean Architecture layering (Application/Domain/Infrastructure) | ⚠️ Auction high, Art medium | High | 5-7d |
| Repository interfaces per aggregate | ⚠️ Payment handlers still inject DbContext | Medium | 2-3d |
| DRY: duplicated Keycloak config, repeated ProgramSetup patterns | ⚠️ Shared helper still pending | Medium | 3d |
| Testing coverage for domain services | ⚠️ Auction 2 tests, Art none | Medium | 2-4d |

## Detailed Findings

### 1. Clean Architecture consistency
- **Update**: Auction now split into Application/Domain/Infrastructure but controllers still depend on `AuctionDbContext` directly for SignalR hub projections.
- **New Risk**: Art service started layering but Program setup still wires repositories directly; leakage between controllers and Infrastructure persists.
- **Recommendation**: Enforce Application handlers for controllers, add MediatR or equivalent request pipeline, move SignalR projections behind application services.
- **Effort**: High (7d) for both services combined.

### 2. Repository Pattern gaps
- **Update**: PaymentService `PayoutCommandHandler` injects `PaymentDbContext` after recent async payout feature.
- **Recommendation**: Introduce `IPayoutRepository` and `ISettlementRepository`; refactor handlers to depend on interfaces for testability.
- **Effort**: Medium (3d).

### 3. DRY around Keycloak configuration
- **Observation**: AppHost provides Keycloak env vars, but `ProgramSetup.cs` across services still parses `KEYCLOAK_*` individually.
- **Recommendation**: Move shared parsing + defaulted audiences into `KunstButikken.ServiceDefaults.AuthenticationExtensions.AddKeycloakAuth(...)` and replace per service blocks.
- **Effort**: Medium (3d) touching all services.

### 4. DI container hygiene
- **Strength**: Services consistently use `AddAdminInfrastructure`, etc., to centralize registrations.
- **Gap**: Some Infrastructure projects new to DI (Auction/Art) need matching `ServiceCollection` extensions once Clean Architecture split is done.
- **Effort**: Bundled with Finding #1.

### 5. Testing & validation
- **Update**: AuctionService now has 2 unit tests, ArtService still lacks meaningful coverage (sample test only).
- **Recommendation**: Add domain tests for bidding, art uploads; incorporate integration-event contract tests.
- **Effort**: Medium (4d to bootstrap tests per service).

## Next Steps / Owners
1. Harden Auction/Art controller boundaries (Backend guild, 5d).
2. Refactor Payment handlers to repository interfaces (Payment squad, 3d).
3. Implement shared Keycloak helper + update ProgramSetup (Platform, 3d).
4. Expand Auction/Art test suites (QA+dev, 4d).
