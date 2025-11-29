# KunstButikken Frontend

Next.js-based frontend for the KunstButikken platform following Clean Architecture, DI via tsyringe, and feature-driven modularity.

## Tech Stack

- **Framework:** Next.js 16 (App Router)
- **Language:** TypeScript
- **UI:** Material UI + Tailwind CSS utilities
- **State:** Redux Toolkit
- **DI & Patterns:** tsyringe + Clean Architecture + Repository Pattern
- **Auth:** Keycloak
- **Payments:** Stripe
- **Testing:** Jest + Testing Library + Playwright

## Architecture & Patterns

- Clean Architecture layers: `application/` (use cases), `infrastructure/` (repositories, HTTP), `presentation/` (providers), `features/` (UI/logic per domain).
- tsyringe container lives in `src/infrastructure/di` and is initialised through `DiProvider` + guardrails ensuring `reflect-metadata` is loaded (`src/app/layout.tsx`, `src/shared/providers/Providers.tsx`).
- Repositories map API DTOs to UI models and are consumed via use cases for both server and client components.
- See `ARCHITECTURE.md` for feature layout details.

```
src/
├── app/                    # Next.js routes & RSCs
├── application/            # Use cases + interfaces
├── features/               # Domain modules (art, auction, auth, etc.)
├── infrastructure/         # DI container, repositories, http client
├── presentation/           # React providers (DI, Keycloak, etc.)
├── shared/                 # Cross-cutting helpers, state
└── styles/                 # Global styles
```

## Getting Started

### Prerequisites

- Node.js 20+
- pnpm 9+
- Backend services via Aspire AppHost (recommended) or running microservices manually

### Installation

```bash
pnpm install
```

### Development

```bash
# Recommended: run through Aspire (boots all services)
cd ..
dotnet run --project KunstButikken.AppHost

# Frontend only (requires env + backend services running)
pnpm dev
```
App runs at `http://localhost:3000`.

### Build

```bash
pnpm build
pnpm start
```

## Environment Variables

Managed via Aspire secrets by default. For standalone work, create `.env.local`:

- `NEXT_PUBLIC_API_GATEWAY`
- `NEXT_PUBLIC_KEYCLOAK_URL`
- `NEXT_PUBLIC_KEYCLOAK_REALM`
- `NEXT_PUBLIC_KEYCLOAK_CLIENT_ID`
- `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY`
- `NEXT_PUBLIC_API_USER`, `NEXT_PUBLIC_API_AUTH`, etc. as needed by providers

See `.env.local.example` for the full list.

## Testing & Quality

```bash
pnpm lint               # ESLint
pnpm test               # Jest unit tests (use cases, repos, slices)
pnpm test:coverage      # Coverage report
pnpm test:e2e           # Playwright E2E
```
- Guardrail in `next.config.js` fails builds if DI polyfill imports are removed.
- Jest covers Clean Architecture layers (use cases/repositories) plus feature slices.

## Scripts

- `pnpm dev` – start dev server
- `pnpm build` / `pnpm start` – production build/start
- `pnpm lint` – lint all sources
- `pnpm test`, `pnpm test:e2e`, `pnpm test:coverage`

## Key Features

- 🎨 Art discovery, seller tooling, and auctions
- 🔐 Keycloak auth (SSR + CSR)
- 💳 Stripe checkout
- 🌍 i18n with locale persistence
- 🧩 DI-backed Clean Architecture for reuse/testing
- 📱 Responsive, dark-mode ready UI

## After Recent Refactors

- All data access flows through repositories + use cases with DI (tsyringe).
- `reflect-metadata` enforced at entry points to prevent runtime DI failures.
- Legacy docs migrated/archived; outdated `.md` files under `docs/archive` removed.
- Expanded Jest coverage across use cases, repositories, and Redux slices.

## Contributing

1. Create/extend feature folders under `src/features` and related use cases under `src/application`.
2. Register new repositories/tokens in `src/infrastructure/di/container.ts`.
3. Add unit tests for each new use case/repository (see `src/application/useCases/__tests__`).
4. Keep README + docs updated after significant architectural changes.
