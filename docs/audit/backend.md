# Backend Architecture Audit (2025-11-29)

## Scope
Services reviewed: AdminService, AuthGateway, PaymentService, UserService, AuctionService, ArtService. Focus areas: Dependency Injection (DI), Clean Architecture adherence, Repository Pattern usage, DRY compliance.

## Summary Table
| Area | Status | Priority | Effort |
| --- | --- | --- | --- |
| DI registrations centralized per service | ✅ | - | - |
| Clean Architecture layering (Application/Domain/Infrastructure) | ⚠️ Incomplete in Art/Auction services | High | 3-5d |
| Repository interfaces per aggregate | ⚠️ Missing for PaymentService (direct DbContext usage) | Medium | 2d |
| DRY: duplicated Keycloak config, repeated ProgramSetup patterns | ⚠️ Improved via AppHost but services still duplicate auth setup | Medium | 3d |
| Testing coverage for domain services | ⚠️ Limited outside Payment/User | Medium | 2d |

## Detailed Findings

### 1. Clean Architecture consistency
- **Issue**: ArtService and AuctionService do not expose Application/Domain/Infrastructure splits (single project). Shared logic (DTOs, validators) sits alongside EF models.
- **Risk**: Makes unit testing and dependency boundaries harder. Violates Clean Architecture goal.
- **Recommendation**: Split into `*.Application`, `*.Domain`, `*.Infrastructure` like Payment/User. Introduce MediatR-style use cases or service interfaces for command/query logic.
- **Effort**: High (5d) for both services combined.

### 2. Repository Pattern gaps
- **Issue**: PaymentService command handlers access DbContext via Infrastructure service without interfaces; AuthGateway proxies external services directly.
- **Risk**: Harder to mock for tests; leaks persistence concerns into Application layer.
- **Recommendation**: Introduce repository interfaces in Application layer with Infrastructure implementations (e.g., `IPayoutRepository`).
- **Effort**: Medium (2d).

### 3. DRY around Keycloak configuration
- **Issue**: Even with AppHost propagation, services still parse `KEYCLOAK_*` individually; `ProgramSetup` files repeat audience parsing logic.
- **Risk**: Drift between services, increased maintenance.
- **Recommendation**: Extract shared extension under `KunstButikken.ServiceDefaults` (e.g., `AddKeycloakAuth(IServiceCollection, IConfiguration)`). Refactor ProgramSetup to call the helper.
- **Effort**: Medium (3d) touching all services.

### 4. DI container hygiene
- **Strength**: Services consistently use `AddAdminInfrastructure`, etc., to centralize registrations.
- **Gap**: Some Infrastructure projects new to DI (Auction/Art) need matching `ServiceCollection` extensions once Clean Architecture split is done.
- **Effort**: Bundled with Finding #1.

### 5. Testing & validation
- **Issue**: Only Payment/User services have test projects; others rely solely on integration tests.
- **Risk**: Regression risk for domain logic.
- **Recommendation**: Add focused unit-test projects per service once layers exist. Start with Auction bidding logic.
- **Effort**: Medium (2d to bootstrap tests per service).

## Next Steps / Owners
1. Plan Clean Architecture split for Art/Auction (Tech Lead, 5d, blocking other refactors).
2. Build shared Keycloak auth helper (Platform team, 3d).
3. Introduce repository interfaces for Payment (Backend dev, 2d).
4. Add baseline tests for Auction domain (QA + dev pairing, 2d).

