# Architecture Documentation

## Overview

This project follows a **feature-based architecture** with domain-driven design principles. Source code is organized in a `src/` directory with features grouped by business domain.

## Project Structure

## Project Structure

```
src/
├── app/                    # Next.js App Router (routes & pages)
├── features/               # Feature modules (domain-driven)
│   ├── admin/             # Admin verification panel
│   ├── art/               # Art browsing, selling, cards
│   ├── auth/              # Authentication (Keycloak)
│   ├── i18n/              # Internationalization
│   ├── navigation/        # Navigation UI components
│   ├── payment/           # Payment processing (Stripe)
│   ├── search/            # Search functionality
│   └── ui/                # Theme & UI components
├── shared/                # Cross-cutting concerns
│   ├── api/               # API utilities
│   ├── providers/         # React providers
│   ├── state/             # Redux store
│   └── utils/             # Common utilities
└── styles/                # Global styles
```

## Feature Organization

Each feature follows this structure:

Each feature follows this structure:

```
features/{feature-name}/
├── components/     # React components
├── api/           # API clients & data fetching
├── types/         # TypeScript types
├── state/         # Redux slices (if needed)
├── lib/           # Feature-specific utilities
└── index.ts       # Public API (barrel export)
```

## Benefits

- **Feature Cohesion** - Related code lives together
- **Scalability** - Easy to add new features independently
- **Maintainability** - Clear organization and ownership
- **Reduced Coupling** - Features are self-contained
- **Domain-Driven** - Aligns with business logic

## Import Patterns

### Feature Imports
```typescript
import { ArtCard } from '@/features/art/components/ArtCard';
import { useKeycloak } from '@/features/auth/lib/keycloak';
import type { UiArt } from '@/features/art/types/art';
```

### Shared Imports
```typescript
import { apiFetch } from '@/shared/api/api';
import { store } from '@/shared/state/store';
import Providers from '@/shared/providers/Providers';
```

## Path Aliases

Configured in `tsconfig.json`:
- `@/*` - Maps to `src/`
- `@/features/*` - Maps to `src/features/`
- `@/shared/*` - Maps to `src/shared/`

---

**Last Updated:** November 15, 2025  
**Status:** Production Ready

