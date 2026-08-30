# Phase 1 — Data Platform & RBAC (In Progress)

> **Status:** ✅ Substantially complete · **Goal:** Secure multi-tenant auth + RBAC, dynamic unit toggle with per-tenant/per-user unit preferences + audit, and a frontend login shell.
> **Canonical spec:** [`../02-reference/codex.tex`](../02-reference/codex.tex) · **Full plan:** [`../01-planning/IMPLEMENTATION_PLAN.md`](../01-planning/IMPLEMENTATION_PLAN.md)
> **Backend design:** [`../03-backend/01-architecture.md`](../03-backend/01-architecture.md) · **DB/RLS:** [`../03-backend/04-database.md`](../03-backend/04-database.md) · **Frontend:** [`../04-frontend/01-architecture.md`](../04-frontend/01-architecture.md)

---

## 1. What was delivered

### 1.1 Domain layer (`Karry.Domain`)

- **Identity/RBAC aggregate:** `User`, `Role` (with owned `RolePermission` → `role_permissions`), `Permission`, `RefreshToken`.
  - `User.Create(tenantId, email, name, passwordHash, isPlatformAdmin, roleId, deviceId, createdBy)`.
  - `Role.Grant` dedupes by `(Resource, Action)`, not `PermissionId`.
  - `RefreshToken` family/rotation model: `FamilyId` + `ParentTokenId` for reuse detection.
- **Tenancy:** `Tenant` aggregate (profile: name, country, currency, timezone, locale).
- **Unit preferences:** `TenantUnitPreference`, `UserUnitPreference` (per-tenant / per-user).
- **Audit:** `AuditLogEntry`.
- Repository contracts extended: `ListAsync(predicate)`, `FirstOrDefaultAsync`, `AnyAsync`.

### 1.2 Application layer (`Karry.Application`)

- **Common abstractions:** `IClock`, `ISecureRandom`, `ICurrentSession`; exceptions (`NotFound`, `Authentication`, `Forbidden`, `Conflict`, `AccountLocked`).
- **Security services:** `IPasswordHasher`, `ITotpService`, `IAccessTokenService`, `FieldMaskingEvaluator` (Read||Write → Visible; Mask → Masked; else Hidden), `RefreshTokenHasher` (SHA-256).
- **Auth use cases:** Login, TwoFactorLogin (challenge), Refresh (rotation + reuse detection), Logout, 2FA enable/verify/disable.
- **Tenancy:** `CreateTenantCommand` — seeds 6 system roles + tenant unit prefs + optional admin.
- **Users:** `CreateUserCommand`, `ListUsersQuery`.
- **Roles:** `CreateRoleCommand`, `ListRolesQuery` (new this session, powers the operator provisioning flow).
- **Units:** `SetUnitPreferencesCommand`.

### 1.3 Infrastructure layer (`Karry.Infrastructure`)

- `PasswordHasher` (ASP.NET Identity PBKDF2), `TotpService` (RFC 6238 HMAC-SHA1 base32).
- EF entity configurations for all Phase-1 tables.
- `RowLevelSecurityInterceptor` — `DbConnectionInterceptor` that sets `app.current_tenant_id` per connection.
- `TokenIssuer` (JWT pair + refresh token persisted).
- Initial migration `20260830150249_InitialIdentityRbac` with **hand-authored RLS policies** on `users`, `roles`, `tenant_unit_preferences`, `user_unit_preferences`, `audit_log`, `machines`, `wear_parts`.
- `DbSeeder` — idempotent seed of the permission catalog + platform super-admin.
- Notes: `AccessTokenService` was moved to the `Api` project (Infrastructure lacks the JWT package); the `(sp, options)` `AddDbContext` overload injects the RLS interceptor.

### 1.4 API layer (`Karry.Api`)

- `ExceptionHandlingMiddleware` — maps application exceptions → HTTP status (incl. `423 Locked` for `AccountLocked`).
- Controllers: `AuthController`, `TenantsController`, `UsersController`, `RolesController` (+ `GET /api/roles` added this session), `UnitsController` (+ `PUT /api/units/preferences`).
- `AccessTokenService` (JWT); `DbSeeder` invoked on `Seed:Enabled`.
- Migration generated + RLS SQL in Up()/Down().

### 1.5 Backend tests (`Karry.Tests` + `Karry.IntegrationTests`)

- **In-memory verification:** `Karry.Tests` now exercises the Application layer against fake repositories/clock/session/hasher/TOTP. 105 tests green.
- **Real-Postgres integration (CI-only):** new `Karry.IntegrationTests` project (its own solution `Karry.IntegrationTests.sln`, kept **out of** `Karry.sln` so local `dotnet test Karry.sln` stays green without a DB):
  - `KarryApiFactory` (`WebApplicationFactory<Program>`) — skips when no Postgres connection string is present.
  - `MilestoneFlowTests` — platform admin logs in → creates tenant (provisions roles + admin) → tenant admin provisions an operator → operator logs in → unit conversion works **both ways** (m³→t and t→m³ round-trip).
  - `SecurityEdgeCaseTests` — 5 wrong passwords then valid password returns `423 Locked`; cross-tenant user isolation via RLS.

### 1.6 CI (`.github/workflows/ci.yml`)

- New **`integration`** job: spins up a `postgres:16` service container, sets `Database__AutoMigrate=true` + `Seed__Enabled=true` + `Seed__AdminPassword`, builds and runs `Karry.IntegrationTests.sln`, uploads TRX results.

### 1.7 Frontend login shell (`src/frontend`)

- `lib/http.ts` — fetch wrapper: Bearer injection, `Idempotency-Key` header for mutating calls, `ApiError` + RFC-7807 problem parsing.
- `features/auth/` — `types.ts`, `api.ts` (login/two-factor/refresh/logout, device-id), `authStore.ts` (zustand + `persist`), `tokenManager.ts` (single-flight refresh + reuse-detection logout), `guards.tsx` (`RequireAuth` / `GuestOnly`), `LoginPage.tsx` (email/password + 2FA step).
- `router.tsx` — `/login` guest route; app shell wrapped in `RequireAuth` (redirects to `/login` with `from` back-link).
- `AppShell.tsx` — signed-in user + role + sign-out button.
- `lib/api.ts` `convertMeasure` now goes through `getAccessToken()` + idempotency.
- `lib/http.test.ts` — idempotency-key + problem-parsing unit tests. 8 frontend tests green.

---

## 2. Milestone demo (Phase 1 acceptance)

1. Platform admin creates a **tenant** → 6 system roles + tenant unit prefs + tenant admin provisioned.
2. Tenant admin logs in (provisions an **operator** via `POST /api/users` with the operator role).
3. Operator logs in.
4. Unit conversion works **both ways** (m³→t and t→m³ round-trip ≈ exact).

Covered end-to-end by `MilestoneFlowTests` against real Postgres in CI.

---

## 3. Verification evidence

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Karry.sln` (local) | 0 warnings, 0 errors |
| Backend tests | `dotnet test Karry.sln --no-build` | 105 passed / 0 failed |
| Integration build (no PG needed) | `dotnet build Karry.IntegrationTests.sln` | 0 warnings, 0 errors |
| Frontend typecheck | `npm run typecheck` | pass |
| Frontend lint | `npm run lint` | pass |
| Frontend format | `npm run format:check` | pass |
| Frontend tests | `npm test` | 8 passed |
| Frontend build (PWA) | `npm run build` | pass |
| CI integration job | (Postgres service container) | runs only in CI |

> The real-Postgres integration tests execute **only in CI** (no Docker/Postgres on the authoring machine). Local Application tests are DB-independent by design.

---

## 4. In scope / out of scope

- **In scope:** identity/tenancy/RBAC, unit preferences + audit, existing `machines`/`wear_parts` RLS, integration tests, frontend login shell.
- **Out of scope (Phase 2+):** site/blast, shifts, warehouse, financial, PKI, weather entities — no domain entities yet, so no migration rows.

---

## 5. Key decisions / gotchas

- **Postgres tests live in CI only.** No Docker/sudo on the authoring box ⇒ `Karry.IntegrationTests` is a separate solution; local `dotnet test Karry.sln` uses in-memory fakes.
- **`Seed:AdminPassword` is mandatory** when seeding — `DbSeeder` throws if absent; CI sets it explicitly.
- **`appsettings.Development.json` forces `Database:AutoMigrate=true`** — running the backend locally without a DB requires `Database__AutoMigrate=false`.
- See [`../08-knowledge/llmhandsoff.md`](../08-knowledge/llmhandsoff.md) entries 015–018 for drill-downs.

---

## 6. Hand-off to remainder of Phase 1 / Phase 2

- Frontend: tenant-management screens (create tenant/users/roles), per-user unit-preference picker wired to `PUT /api/units/preferences`, 2FA enrollment UI, route guard per role.
- Swagger/OpenAPI niceties + API docs refresh in `03-backend/03-api.md`.
- Phase 2 entities (site/blast, shifts, warehouse, financial, PKI, weather) + their migrations, warehouse/site RLS.
