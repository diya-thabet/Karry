# Frontend Architecture (React PWA)

> **Layer:** `src/frontend/` · **Extracted from** the cross-cutting [`../architecture.md`](../architecture.md).

---

```
src/frontend/
├── index.html · vite.config.ts · tailwind.config.js · postcss.config.js
├── public/                 # favicon.svg, pwa-192x192.svg
├── src/
│   ├── main.tsx            # boots React, registers service worker
│   ├── app/router.tsx      # React Router (createBrowserRouter) + AppShell
│   ├── components/layout/AppShell.tsx
│   ├── features/
│   │   ├── home/HomePage.tsx
│   │   └── units/          # UnitToggle.tsx, convert.ts (pure), convert.test.ts
│   ├── lib/api.ts          # fetch-based API client (VITE_API_BASE_URL or /api proxy)
│   └── vite-env.d.ts
```

## Key decisions

- **PWA / offline-first:** `vite-plugin-pwa` (auto generateSW). Service worker + Web App Manifest.
- **Dev proxy:** `/api → localhost:5000`, `/engine → localhost:8000` (via `server.proxy`, dev only).
- **Path alias:** `@/* → src/*`.
- **State/offline:** Zustand (global state) + Dexie (IndexedDB) — pulled in for the Phase-2 offline shift queue.
- **Styling:** Tailwind (`primary #142d55`, `accent #2980b9`; uses default `slate` scale).

## Code organization rules

- Feature-first: each `src/features/<name>/` owns its components, pure logic (`convert.ts`), API calls, and tests.
- `src/lib/` for shared infrastructure (API client, types).
- Keep pure, non-React logic dependency-free and unit-testable (see `features/units/convert.ts` + `convert.test.ts`).

## Tooling & quality gates

| Check | Command |
|---|---|
| TypeScript (strict) | `npm run typecheck` |
| Lint (flat config ESLint) | `npm run lint` |
| Format (Prettier) | `npm run format:check` |
| Tests (Vitest) | `npm test` |
| Build (PWA) | `npm run build` |
