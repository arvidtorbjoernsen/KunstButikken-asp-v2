# components/ — Feature-first organization

This folder contains shared, reusable UI that can be used across multiple routes in the Next.js app. We follow a feature-first organization: group related UI, hooks, and (feature-specific) types by domain, rather than by technical layer.

Key domains today:
- nav/ — Navigation bar and related UI (brand, links, menus, drawers)
- auth/ — Auth-related client components (Sign in, Register, Error UI)
- cards/ — Common cards (e.g., ArtCard)
- i18n/ — Translation provider and utilities
- providers/ — App-wide React providers
- ui/ — Generic UI widgets (e.g., ThemeToggle)
- data/ — Small data fixtures used in the UI

Conventions
- Keep related pieces together where it makes sense: component + small hook + types used only by that component/domain can live together inside the domain folder.
- Prefer colocating route-only components under app/<route>/_components/ instead of placing them here. This folder should stay focused on reused building blocks.
- Use optional “barrel” files (index.ts) in each domain to expose a clear public surface and enable short imports.
- If multiple domains need the same hook or type, start by colocating; extract to top-level hooks/ or types/ only after reuse emerges.

Naming
- Components: PascalCase, e.g., ArtCard.tsx, NavbarUserMenu.tsx
- Hooks: useSomething.ts
- Folders: domain name in singular (auth, nav, i18n, providers, ui)
- Client components can use the Client suffix to signal browser-only usage (e.g., SignInClient.tsx)

Imports
- Absolute imports via @/ alias:
  - From app/: import shared UI using either their barrel or direct file import.
    - Example (barrel): import Navbar from "@/components/nav"
    - Example (file): import Navbar from "@/components/nav/Navbar"
- When adding a new domain, add an index.ts barrel that re-exports the public components, and keep internal files private.

Examples
- nav/index.ts can export Navbar, NavbarLinks, NavbarUserMenu, etc. Consumers then import from "@/components/nav".
- auth/index.ts can export SignInClient, RegisterClient, and AuthErrorClient.

Notes
- Avoid moving files just to satisfy organization. Make changes incrementally and update imports in a single PR when you do move code.
- Keep churn low: colocate route-only UI under app/<route>/ when it’s not reused.

For broader guidance and the proposed target layout, see: ../../docs/CONTRIBUTING.md
