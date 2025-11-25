# app/auth/_components — Route-only components

These components are used only by pages under the `/auth` route (e.g., `signin/page.tsx`, `register/page.tsx`, `error/page.tsx`). They are colocated here to reduce noise in `components/` and make the relationship to their routes obvious.

Guidelines
- If a component becomes reused outside `/auth`, move it to `components/auth/` (or a feature module) and update imports in one PR.
- Keep logic minimal; these are thin client-side helpers that orchestrate redirects or small UI for the route.
- Prefer importing these within the `/app/auth/*` pages like:
  - `import SignInClient from "@/app/auth/_components/SignInClient"`.

Note: For backward compatibility, `components/auth/*` currently re-export these components. This allows gradual migration of imports.