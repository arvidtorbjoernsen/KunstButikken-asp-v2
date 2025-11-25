# KunstButikken (asp)

KunstButikken is a microservice-based marketplace for art. This repository contains the backend services (ASP.NET), two frontend applications (a modern Next.js app and a legacy Angular app), and developer tooling for running and testing the system locally.

This README gives a concise orientation for developers and maintainers.

## Repository layout (top-level)

- Solution and repo metadata:
  - `KunstButikken-asp.sln` — Visual Studio / dotnet solution that ties services together
  - `Directory.Packages.props` / `NuGet.config` — package/version configuration
- Backend services (ASP.NET Core microservices):
  - `KunstButikken.AdminService`
  - `KunstButikken.ArtService`
  - `KunstButikken.AuctionService`
  - `KunstButikken.AuthGateway`
  - `KunstButikken.PaymentService`
  - `KunstButikken.UserService`
  - `KunstButikken.AppHost` — host/aggregator for composing services in local runs
- Frontend:
  - `KunstButikken.Frontend` — Next.js / React application (primary modern frontend)
  - `KunstButikken.Frontend-Ang` — legacy Angular application (kept for reference/compat)
- Helpers & tooling:
  - `scripts/`, `tools/`, CI config, and local dev helpers
  - `coverage/` folder for test artifacts

## Architecture (high level)

- Microservices communicate over HTTP and are intended to be independently runnable.
- `AuthGateway` centralizes authentication decisions for services (Keycloak is used in many environments).
- The Next.js frontend is the primary UI surfaced for users; the Angular app is legacy and maintained for reference or migration tasks.

## Developer quick-start

Prerequisites

- .NET SDK matching the solution (check `Directory.Packages.props` or `global.json` if present).
- Node.js (recommended v18+)
- `pnpm` (used by the Next.js frontend)
- Optional: Docker (for DBs, Azurite, or other services)

Run the backend services

1. Build the whole solution:

```bash
dotnet build KunstButikken-asp.sln
```

2. Run a single service (example: ArtService):

```bash
cd KunstButikken.ArtService
dotnet run --project .
```

Run the Next.js frontend (development)

```bash
cd KunstButikken.Frontend
pnpm install
pnpm run dev
```

Build the Next.js frontend for production

```bash
cd KunstButikken.Frontend
pnpm install
pnpm run build
```

Running tests

- Frontend tests: use the scripts in `KunstButikken.Frontend` (see its `package.json`, commonly `pnpm test`).

Environment and secrets

- Some services rely on local environment variables or user secrets (see `setup-user-secrets.sh` and `setup` scripts).
- Keycloak/identity configuration is expected for end-to-end auth flows; for local development there are dev bypasses and seeded accounts in some services.
