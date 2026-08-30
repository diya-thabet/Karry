# Frontend Architecture (React PWA)

> **Layer:** `src/frontend/` · **Extracted from** the cross-cutting [`../architecture.md`](../architecture.md).

---

```
src/frontend/
├── index.html · vite.config.ts · tailwind.config.js · postcss.config.js
├── public/                 # favicon.svg, pwa-192x192.svg
├── src/
│   ├── main.tsx            # boots React, registers service worker
│   ├── app/router.tsx      # React Router (createBrowserRouter) + RequireAuth/GuestOnly
│   ├── components/layout/AppShell.tsx  # signed-in user + role + sign-out
│   ├── features/
│   │   ├── auth/           # login + 2FA, store, token refresh, guards
│   │   ├── home/HomePage.tsx
│   │   └── units/          # UnitToggle.tsx, convert.ts (pure), convert.test.ts
│   ├── lib/
│   │   ├── api.ts          # feature API calls (convertMeasure)
│   │   └── http.ts         # fetch wrapper: Bearer, idempotency, ApiError + parseProblem
│   └── vite-env.d.ts
```

## Key decisions

- **PWA / offline-first:** `vite-plugin-pwa` (auto generateSW). Service worker + Web App Manifest.
- **Dev proxy:** `/api → localhost:5000`, `/engine → localhost:8000` (via `server.proxy`, dev only).
- **Path alias:** `@/* → src/*`.
- **State/offline:** Zustand (global state) + Dexie (IndexedDB) — pulled in for the Phase-2 offline shift queue.
- **Styling:** Tailwind (`primary #142d55`, `accent #2980b9`; uses default `slate` scale).

## Auth & security (Phase 1)

- **Auth store** (`features/auth/authStore.ts`): Zustand + `persist` (tokens/email/role persist across reloads).
- **Route guards** (`features/auth/guards.tsx`): `RequireAuth` redirects anonymous users to `/login`, `GuestOnly` redirects signed-in users away from `/login`.
- **Token refresh** (`features/auth/tokenManager.ts`): single-flight refresh; a failed rotation clears the session (reuse-detection logout).
- **HTTP client** (`lib/http.ts`): injects `Authorization: Bearer`, adds `Idempotency-Key` for mutating calls, normalizes RFC-7807 errors into `ApiError`.
- **Login page** (`features/auth/LoginPage.tsx`): email/password → optional 2FA challenge step → token session; uses a persisted `deviceId`.

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
