# Karry Platform — System Architecture

> **Audience:** Any engineer or LLM joining the project. This is the cross-cutting architecture overview.
> **Layer-specific docs:** [`03-backend/01-architecture.md`](03-backend/01-architecture.md) · [`04-frontend/01-architecture.md`](04-frontend/01-architecture.md) · [`05-math-engine/01-architecture.md`](05-math-engine/01-architecture.md) · [`06-infrastructure/01-architecture.md`](06-infrastructure/01-architecture.md)
> **Companions:** [`02-reference/codex.tex`](02-reference/codex.tex) (domain/math spec) · [`01-planning/IMPLEMENTATION_PLAN.md`](01-planning/IMPLEMENTATION_PLAN.md) (phased build plan) · [`08-knowledge/llmhandsoff.md`](08-knowledge/llmhandsoff.md) (lessons learned).

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

## 3. Layer Architecture Breakdown

Deep-dive architecture for each service lives in its own doc (search by layer):

| Layer | Architecture doc |
|---|---|
| **Backend (.NET 9)** | [`03-backend/01-architecture.md`](03-backend/01-architecture.md) (+ domain `02`, API `03`, database `04`) |
| **Frontend (React PWA)** | [`04-frontend/01-architecture.md`](04-frontend/01-architecture.md) |
| **Math Engine (Python)** | [`05-math-engine/01-architecture.md`](05-math-engine/01-architecture.md) |
| **Infrastructure / DevOps** | [`06-infrastructure/01-architecture.md`](06-infrastructure/01-architecture.md) |

Core principles:
- Backend: Clean/onion layering with **strictly inward** dependency flow (`Api → Application → Domain`, `Api → Infrastructure → Application/Domain`).
- Math engine: **stateless HTTP microservice**; the .NET API owns persistence, the engine computes and returns JSON.
- Frontend: offline-first PWA with feature-first code organization.

---

## 4. Data Model (Phase 1 target — entities planned)

The Phase 0 backend already seeds the domain types; Phase 1 adds the full schema. Planned core tables (see [`01-planning/IMPLEMENTATION_PLAN.md`](01-planning/IMPLEMENTATION_PLAN.md) §4.1, or the backend [`03-backend/04-database.md`](03-backend/04-database.md)):

- **Identity/Tenancy**: `tenants`, `users`, `roles`, `permissions`, `role_permissions`
- **Site/Blast**: `sites`, `blast_patterns` (PostGIS `geometry`), `blasts`, `production_runs`
- **Equipment**: `machines`, `machine_nodes` (graph edges JSONB), `wear_parts`, `wear_usage_logs`, `maintenance_events`
- **Shifts/Verification**: `shifts`, `shift_entries`, `entry_audit_overrides`, `scale_tickets`
- **Warehouse**: `warehouses`, `inventory_items`, `stock_movements`, `transfer_waybills`, `asset_transfers`, `physical_inventory_audits`
- **Financial**: `ledger_entries` (append-only), `loans`, `amortization_schedules`, `operational_costs`
- **PKI**: `signatures`, `signed_documents`
- **Weather/Analytics**: `weather_forecasts`, `production_telemetry`

### 4.1 Tenancy strategy

- Every tenant-scoped table gets a `tenant_id` + PostgreSQL Row-Level Security (RLS).
- `TenantContextMiddleware` exposes the JWT `tenant_id` claim via `HttpContext.Items`.
- **Phase 1 deliverable**: connection-level SQL interceptor that issues `SET app.current_tenant_id = '<id>'` per session, plus RLS policies:
  ```sql
  CREATE POLICY tenant_isolation_policy ON table_name
  USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
  ```

---

## 5. Cross-Cutting Concerns

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

## 6. Build, Run & CI

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

## 7. Evolution Roadmap (next phases)

| Phase | Scope |
|---|---|
| **1** | Full schema + migrations + seed; Identity/RBAC with JWT+refresh+2FA; field masking; RLS enforcement |
| **2** | Field PWA: shift logging, dual-shift continuity, controller approval pipeline, PKI e-signatures, weighbridge tickets, offline queue |
| **3** | Hybrid maintenance engine (RUL scheduling, procurement triggers), Magasin Général warehouse + transfers + inventory audits |
| **4** | Analytics dashboards, loan/weather graphs, Executive AI copilot (RAG) |
| **5-6** | Hardening/security/observability, deployment + pilot rollout |

---

## 8. Style & Contribution Notes

- Per-layer style rules live in each layer's architecture doc ([backend](03-backend/01-architecture.md), [frontend](04-frontend/01-architecture.md), [math](05-math-engine/01-architecture.md)).
- When you change architecture, update the relevant **layer doc** (this overview if cross-cutting) **and** append to [`08-knowledge/llmhandsoff.md`](08-knowledge/llmhandsoff.md).
- Document each completed phase in `07-execution/phaseN-<slug>.md`.
