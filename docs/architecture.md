# Karry Platform — System Architecture

> **Audience:** Any engineer or LLM joining the project. This document is the single source of truth for *how the system is structured and why*.
> **Companions:** [`docs/codex.tex`](codex.tex) (domain/math spec) · [`docs/IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) (phased build plan) · [`docs/llmhandsoff.md`](llmhandsoff.md) (lessons learned).

---

## 1. System at a Glance

**Karry** is a **multi-tenant, offline-first SaaS** for aggregate quarries, sand washing plants, and open-pit mining. It digitizes the full chain:

```
Blast bench → Crusher/screen plant → Weighbridge → Warehouses → Shift controllers → Executive ledger + analytics/AI
```

Three separately-built services (a monorepo, but independently deployable):

| Service | Language/Stack | Role |
|---|---|---|
| **Backend API** (`src/backend`) | .NET 9 / ASP.NET Core / EF Core / PostgreSQL+PostGIS | Domain logic, auth/RBAC, persistence, business APIs |
| **Math Engine** (`src/math-engine`) | Python 3 / FastAPI / NumPy | Deterministic math: conveyor physics, RUL, (future) DCG mass-balance & solvency |
| **Frontend PWA** (`src/frontend`) | React 18 / TS / Vite / Tailwind / Dexie | Offline-first field UI, dashboards, e-signature capture |

Supporting infra: **Redis** (cache), **MinIO / S3** (documents/signatures), **Docker Compose** (dev), **GitHub Actions** (CI).

---

## 2. High-Level Logical Architecture

```
                   ┌──────────────────────────────────────────────┐
                   │              CLIENT LAYER (PWA)              │
                   │   React 18 + TS · offline IndexedDB (Dexie)  │
                   │   UnitToggle · (future) e-sign, dashboards   │
                   └───────────────────────┬──────────────────────┘
                                           │  HTTPS / REST (JSON) · WebSocket (future)
                   ┌───────────────────────▼──────────────────────┐
                   │              BACKEND API (.NET 9)            │
                   │  JWT auth · RBAC enforcer · tenant context   │
                   │  MediatR + FluentValidation use cases        │
                   │  Domain / Application / Infrastructure layers│
                   └───────┬──────────────────────────┬───────────┘
                           │                          │  Typed HttpClient
              ┌────────────▼────────────┐   ┌─────────▼─────────────┐
              │  PostgreSQL 16 + PostGIS│   │  MATH ENGINE (Python) │
              │  EF Core · RLS policies │   │  FastAPI + NumPy      │
              │  Redis cache            │   │  /engine/conveyor,    │
              │  MinIO/S3 objects       │   │  /engine/rul, ...     │
              └─────────────────────────┘   └───────────────────────┘
```

---

## 3. Backend Architecture (.NET 9)

Clean/onion layering. **Dependency flow is strictly inward:** `Api → Application → Domain` and `Api → Infrastructure → Application/Domain`.

### 3.1 Projects & responsibilities

```
src/backend/
├── Karry.sln
├── Directory.Build.props          # shared: net9.0, nullable, TreatWarningsAsErrors
├── Karry.Domain/                  # NO external deps — pure domain
│   ├── Common/                    # BaseEntity, ValueObject, ITenantScoped,
│   │                              # IRepository<T>, IUnitOfWork, ICurrentTenant/User, IAuditableEntity
│   ├── Tenants/Tenant.cs
│   ├── Units/Measure.cs           # m³ ↔ Tonnes toggle (value object)
│   ├── Equipment/Machine.cs       # "graph node engine" + downstream edges
│   └── Maintenance/WearPart.cs    # hybrid maintenance + RUL
├── Karry.Application/             # use cases, MediatR, FluentValidation
│   ├── DependencyInjection.cs
│   └── Units/Commands/            # ConvertMeasure (request, validator, command+handler)
├── Karry.Infrastructure/          # EF Core, repos, tenant context, redis, math client
│   ├── Persistence/               # KarryDbContext, configurations, factory, GenericRepository
│   └── Context/TenantContext.cs   # ICurrentTenant + ICurrentUser from HttpContext
├── Karry.MathEngine.Client/       # typed HttpClient + options for the Python engine
└── Karry.Api/                     # host: JWT, Serilog, Swagger, middleware, controllers
```

### 3.2 Key architectural decisions

| Decision | Rationale |
|---|---|
| **DDD layers, inward-only deps** | Testability, separation of concerns; domain pure C# |
| **MediatR + FluentValidation** in Application | Decouples controllers from use cases; validation centralized |
| **EF Core + PostgreSQL/PostGIS** | Spatial blast grids; RLS; mature relational modeling |
| **Repository + UnitOfWork via `DbContext`** | `KarryDbContext` itself implements `IUnitOfWork`; generic `IRepository<T>` |
| **Tenant isolation** | `ICurrentTenant` resolved per request; domain entities implement `ITenantScoped`; DB writes stamped on add. (Full RLS enforcement: Phase 1) |
| **Math logic delegated to Python** | Numerical fidelity + NumPy for future DCG solvers; isolated, independently scaleable |
| **Value object for units (`Measure`)** | Encapsulates the dynamic unit toggle semantics in one testable place |

### 3.3 DI composition

- `Karry.Application.DependencyInjection.AddApplication()` → MediatR + validators.
- `Karry.MathEngine.Client.DependencyInjection.AddMathEngineClient(config)` → `MathEngineOptions` + `HttpClient<KarryMathEngineClient>`.
- `Karry.Infrastructure.DependencyInjection.AddInfrastructure(config)` → `IHttpContextAccessor`, `TenantContext` (as both `ICurrentTenant` + `ICurrentUser`), `KarryDbContext`, `GenericRepository` for `IRepository<>`, `IUnitOfWork`, Redis.
- `Program.cs` wires everything, adds JWT bearer + Swagger, Serilog, and calls `MigrateDatabaseAsync` (auto-migrate on in Development).

### 3.4 Persistence details

- `KarryDbContext : DbContext, IUnitOfWork`
- Configurations via `IEntityTypeConfiguration<T>` (`Machine`, `WearPart`, `Tenant`) — table-per-entity (`machines`, `wear_parts`, `tenants`).
- `KarryDbContextFactory : IDesignTimeDbContextFactory<KarryDbContext>` for headless EF migrations.
- Npgsql + `UseNetTopologySuite()` (PostGIS geometry support ready for blast grids).

### 3.5 Current API surface (Phase 0)

| Endpoint | Method | Auth | Purpose |
|---|---|---|---|
| `/api/units/convert` | POST | JWT | Dynamic m³ ↔ Tonnes conversion (M = V × ρ × κ_moisture) |
| `/swagger` | GET | – | Swagger UI |

---

## 4. Math Engine Architecture (Python)

Decision: **stateless HTTP microservice** behind the .NET API — the API is the single source of truth for persistence; the math engine computes and returns JSON.

```
src/math-engine/
├── pyproject.toml          # fastapi, uvicorn, numpy, pydantic; dev: pytest, httpx, ruff
├── conftest.py             # ensures `app` package importable by pytest
├── app/
│   ├── main.py             # FastAPI app: /health, /engine/conveyor, /engine/rul
│   ├── core/
│   │   ├── conveyor.py     # compute_q_belt(...)
│   │   └── rul.py          # compute_rul_days(...)
│   └── schemas/__init__.py # Pydantic request/response models (snake_case)
└── tests/                  # unit + API tests
```

### 4.1 Contracts (snake_case JSON, mirrors codex)

- `POST /engine/conveyor` → `{"q_nominal", "phi_wear", "psi_inclination", "omega_weather"}` → `{ "q_belt" }`
- `POST /engine/rul` → `{ "rating_usage", "accumulated_usage", "daily_usage", "rating_mass", "processed_mass", "daily_mass", "bond_abrasion_index" }` → `{ "rul_days" }`

The .NET `KarryMathEngineClient` uses `JsonPropertyName` to map these snake_case fields.

---

## 5. Frontend Architecture (React PWA)

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

- **PWA/offline-first**: `vite-plugin-pwa` (auto generateSW). Service worker + manifest.
- **Dev proxy**: `/api → localhost:5000`, `/engine → localhost:8000` (only in dev via `server.proxy`).
- **Path alias**: `@/* → src/*`.
- **State/offline**: Zustand + Dexie (IndexedDB) pulled in for Phase-2 offline shift queue.
- **Styling**: Tailwind (`primary #142d55`, `accent #2980b9`; default `slate` scale).

---

## 6. Data Model (Phase 1 target — entities planned)

The Phase 0 backend already seeds the domain types; Phase 1 adds the full schema. Planned core tables (see `IMPLEMENTATION_PLAN.md §4.1`):

- **Identity/Tenancy**: `tenants`, `users`, `roles`, `permissions`, `role_permissions`
- **Site/Blast**: `sites`, `blast_patterns` (PostGIS `geometry`), `blasts`, `production_runs`
- **Equipment**: `machines`, `machine_nodes` (graph edges JSONB), `wear_parts`, `wear_usage_logs`, `maintenance_events`
- **Shifts/Verification**: `shifts`, `shift_entries`, `entry_audit_overrides`, `scale_tickets`
- **Warehouse**: `warehouses`, `inventory_items`, `stock_movements`, `transfer_waybills`, `asset_transfers`, `physical_inventory_audits`
- **Financial**: `ledger_entries` (append-only), `loans`, `amortization_schedules`, `operational_costs`
- **PKI**: `signatures`, `signed_documents`
- **Weather/Analytics**: `weather_forecasts`, `production_telemetry`

### 6.1 Tenancy strategy

- Every tenant-scoped table gets a `tenant_id` + PostgreSQL Row-Level Security (RLS).
- `TenantContextMiddleware` exposes the JWT `tenant_id` claim via `HttpContext.Items`.
- **Phase 1 deliverable**: connection-level SQL interceptor that issues `SET app.current_tenant_id = '<id>'` per session, plus RLS policies:
  ```sql
  CREATE POLICY tenant_isolation_policy ON table_name
  USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
  ```

---

## 7. Cross-Cutting Concerns

| Concern | Approach |
|---|---|
| **Multi-tenancy** | `ITenantScoped` stamping + RLS (Phase 1) + `ICurrentTenant` |
| **Auditing** | Append-only `audit_log` (Phase 1); every approval/override logged with controller identity, original/modified value, timestamp |
| **Idempotency / offline sync** | `Idempotency-Key` header; optimistic UI + server-authoritative conflict resolution (Phase 2) |
| **Configuration** | `appsettings.json` + env vars; `JWT_SECRET` from env; `.env` for compose |
| **Secrets** | `.gitignore` excludes `.env`, keys, PEM/PFX; `COPY .env.example` only |
| **Warnings-as-errors** | `Directory.Build.props` `TreatWarningsAsErrors=true` — keeps C# clean |
| **i18n/multi-currency** | Planned: EN/FR, USD/EUR/XOF/CAD — Phase 1+ |

---

## 8. Build, Run & CI

### Local run
```bash
make bootstrap   # env templates + npm install + docker build
make up          # full compose stack (Postgres, Redis, MinIO, API, math, frontend)
```
- Frontend: http://localhost:5173
- API Swagger: http://localhost:5000/swagger
- Math engine: http://localhost:8000/docs
- MinIO console: http://localhost:9001

### CI (GitHub Actions — `.github/workflows/ci.yml`)
Four jobs: `backend`, `frontend`, `math-engine`, `docker-images`. Runs on push/PR to `main`/`develop`.

---

## 9. Evolution Roadmap (next phases)

| Phase | Scope |
|---|---|
| **1** | Full schema + migrations + seed; Identity/RBAC with JWT+refresh+2FA; field masking; RLS enforcement |
| **2** | Field PWA: shift logging, dual-shift continuity, controller approval pipeline, PKI e-signatures, weighbridge tickets, offline queue |
| **3** | Hybrid maintenance engine (RUL scheduling, procurement triggers), Magasin Général warehouse + transfers + inventory audits |
| **4** | Analytics dashboards, loan/weather graphs, Executive AI copilot (RAG) |
| **5-6** | Hardening/security/observability, deployment + pilot rollout |

---

## 10. Style & Contribution Notes

- **.NET**: file-scoped namespaces, expression-bodied where single-line, no XML-doc warnings (`NoWarn=1591`), `TreatWarningsAsErrors`.
- **Frontend**: ESLint flat config (typescript-eslint), Prettier (semi, single-quote, trailing-comma all), Vitest.
- **Python**: Ruff (`E,F,W,I,UP,B,SIM`, line-length 120), a `conftest.py` at project root so pytest can import `app`.
- When you change architecture, update **this** file (`architecture.md`) **and** append to `llmhandsoff.md`.
- Document each completed phase in a `docs/phaseN-<slug>.md` log.
