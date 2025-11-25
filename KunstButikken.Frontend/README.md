# KunstButikken Frontend

Next.js-based frontend for the KunstButikken art marketplace platform.

## Tech Stack

- **Framework:** Next.js 16 with App Router
- **UI:** Material-UI (MUI) with SSR support
- **State:** Redux Toolkit
- **Auth:** Keycloak (via custom integration)
- **Payment:** Stripe
- **Language:** TypeScript
- **Styling:** Tailwind CSS + Material-UI

## Architecture

This project uses a **feature-based architecture** with domain-driven design. See [ARCHITECTURE.md](./ARCHITECTURE.md) for details.

```
src/
├── app/                # Next.js routes
├── features/          # Feature modules (art, auth, admin, etc.)
├── shared/            # Cross-cutting concerns
└── styles/            # Global styles
```

## Getting Started

### Prerequisites

- Node.js 18+
- pnpm (recommended) or npm
- Running backend services (via Aspire AppHost)

### Installation

```bash
pnpm install
```

### Development

```bash
# Run via Aspire (recommended - includes all services)
cd ..
dotnet run --project KunstButikken.AppHost

# Or run standalone (requires backend services running)
pnpm dev
```

The app will be available at `http://localhost:3000`

### Build

```bash
pnpm build
pnpm start
```

## Environment Variables

Environment variables are automatically injected by the Aspire AppHost. For standalone development, copy `.env.local.example` to `.env.local`.

**Required Variables:**
- `NEXT_PUBLIC_API_GATEWAY` - API Gateway URL
- `NEXT_PUBLIC_KEYCLOAK_URL` - Keycloak server URL
- `NEXT_PUBLIC_KEYCLOAK_REALM` - Keycloak realm
- `NEXT_PUBLIC_KEYCLOAK_CLIENT_ID` - Keycloak client ID
- `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` - Stripe publishable key

See `.env.local.example` for the complete list.

## Testing

```bash
# Run unit tests
pnpm test

# Run E2E tests
pnpm test:e2e

# Generate coverage report
pnpm test:coverage
```

## Scripts

- `pnpm dev` - Start development server
- `pnpm build` - Build for production
- `pnpm start` - Start production server
- `pnpm lint` - Run ESLint
- `pnpm test` - Run Jest tests
- `pnpm test:e2e` - Run Playwright E2E tests

## Features

- 🎨 Browse and purchase artwork
- 🔐 Keycloak authentication
- 🛒 Shopping cart functionality
- 💳 Stripe payment integration
- 🌍 Multi-language support (i18n)
- 🎭 Auction bidding system
- 👤 User profile management
- 🔍 Search and filtering
- 📱 Responsive design
- 🌙 Dark mode support

## Project Structure

For detailed information about the project architecture and conventions, see:
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Feature-based architecture overview
- [docs/archive/](./docs/archive/) - Historical migration documentation

## Contributing

When adding new features:
1. Create a new feature module in `src/features/`
2. Follow the established feature structure (components, api, types, state)
3. Use barrel exports (`index.ts`) for clean imports
4. Add tests alongside your features
5. Update this README if adding major functionality


