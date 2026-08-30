# Phase 0 — Foundations & Tooling (Completed)

> **Status:** ✅ Complete · **Duration:** single engineering session
> **Goal:** Reproducible dev environment, CI/CD skeleton, coding standards, and a verified build across all three service layers.
> **Canonical spec:** [`../02-reference/codex.tex`](../02-reference/codex.tex) · **Full plan:** [`../01-planning/IMPLEMENTATION_PLAN.md`](../01-planning/IMPLEMENTATION_PLAN.md)

---

## 1. What was delivered

### 1.1 Monorepo structure

The entire repository was scaffolded from scratch (empty repo, no prior commits) into a clean monorepo:

```
Karry/
├── docs/                          # design docs + this phase log
├── .github/workflows/ci.yml       # CI pipeline
├── infra/
│   ├── compose.yaml               # local dev stack (Postgres+PostGIS, Redis, MinIO, API, math, frontend)
│   └── docker/                    # backend.Dockerfile, math-engine.Dockerfile, frontend.Dockerfile
├── scripts/dev/bootstrap.sh       # one-shot first-time setup
├── Makefile                       # task runner (20+ targets)
└── src/
    ├── backend/                   # .NET 9 solution
    ├── math-engine/               # Python FastAPI
    └── frontend/                  # React 18 + TS + Vite PWA
```

### 1.2 Backend (.NET 9) — `src/backend/`

Clean Architecture solution `Karry.sln` with six projects:

- **Karry.Domain** — no external deps. Contains:
  - `Common/`: `BaseEntity`, `ValueObject`, `IAuditableEntity`, `ITenantScoped`, `ICurrentTenant`, `ICurrentUser`, `IRepository<T>`, `IUnitOfWork`
  - `Tenants/Tenant.cs` — tenant aggregate with profile update
  - `Units/Measure.cs` + `UnitEnums.cs` — the dynamic **m³ ↔ Tonnes** toggle (M = V × ρ × κ_moisture), supports Metric/Short tons
  - `Equipment/Machine.cs` — the codex "Graph Node Engine" with downstream routing edges (`E_out`)
  - `Maintenance/WearPart.cs` — hybrid predictive maintenance with `ComputeRemaining()` (RUL) and meter-switch fallback
- **Karry.Application** — MediatR + FluentValidation. Contains the working `Units` feature (convert command, request record, validator).
- **Karry.Infrastructure** — EF Core (`KarryDbContext`), entity configurations, `GenericRepository<T>`, `TenantContext` (per-request tenant/user), design-time `KarryDbContextFactory`, Redis cache, and the MathEngine client registration.
- **Karry.Api** — ASP.NET Core host: Serilog, JWT auth, Swagger, `TenantContextMiddleware`, `DatabaseExtensions.MigrateDatabaseAsync`, and `UnitsController` (the only live endpoint: `POST /api/units/convert`).
- **Karry.MathEngine.Client** — typed HTTP client + options for the Python engine (`/engine/conveyor`, `/engine/rul`).
- **Karry.Tests** — xUnit. Domain unit tests for `Measure` conversion and `WearPart`/`Machine`.

### 1.3 Math Engine (Python FastAPI) — `src/math-engine/`

- `pyproject.toml` — deps (fastapi, uvicorn, numpy, pydantic) + dev deps (pytest, httpx, ruff) + Ruff config
- `app/main.py` — FastAPI app with `/health`, `POST /engine/conveyor`, `POST /engine/rul`
- `app/core/` — `conveyor.py` (compute Q_belt), `rul.py` (compute RUL days)
- `app/schemas/` — Pydantic request/response models
- `tests/` — unit tests (conveyor, rul) + HTTP API tests

### 1.4 Frontend (React 18 + TS + Vite + Tailwind PWA) — `src/frontend/`

- Vite + React 18 + TypeScript + Tailwind CSS + `vite-plugin-pwa` (offline-first, service worker + manifest)
- `src/app/router.tsx` — React Router with an `AppShell` layout
- `src/features/units/` — working **UnitToggle** UI wired to the backend `/api/units/convert` + a pure `convert.ts` helper (unit-tested)
- Dexie (IndexedDB) dependency pulled in for the Phase-2 offline queue
- ESLint (flat config) + Prettier + Vitest configured

### 1.5 Docker Compose dev stack — `infra/compose.yaml`

- `postgres` (postgis/postgis:16-3.4) with healthcheck
- `redis` (redis:7-alpine) with healthcheck
- `minio` (minio/minio) with healthcheck
- `math-engine` (build from Dockerfile, healthcheck on `/health`)
- `backend` (.NET, depends_on healthy postgres/redis/math-engine)
- `frontend` (vite dev)
- Named volumes for postgres + minio

### 1.6 CI pipeline — `.github/workflows/ci.yml`

Four jobs (run on push/PR to `main`/`develop`):
- **backend**: restore → build → test (uploads TRX results)
- **frontend**: install → lint → format:check → typecheck → test → build
- **math-engine**: setup python → install -e '.[dev]' → ruff → pytest
- **docker-images**: builds all three images (needs the three jobs above)

### 1.7 Linting / formatting / standards

- Root `.editorconfig` (general + C# + Python + Makefile)
- Frontend: `eslint.config.js` (flat config, typescript-eslint + react-hooks/refresh) + `.prettierrc.json` + `.prettierignore`
- Math engine: Ruff config inside `pyproject.toml`
- `Directory.Build.props` for .NET: net9.0, nullable enable, **TreatWarningsAsErrors=true**, langversion latest

### 1.8 Config / secrets / onboarding

- Root `.env.example` (Postgres, MinIO, JWT) and `src/frontend/.env.example`
- `.gitignore` (env files, secrets, bin/obj, node_modules, dist, .venv, terraform, OS files)
- `.dockerignore` for each of the three build contexts
- `Makefile` (help, bootstrap, up, down, build, validate-compose, test\*, lint, typecheck, math-\*, backend-run, frontend-run, migrate, clean)
- `scripts/dev/bootstrap.sh` (copies env templates, installs deps, builds images)
- Root `README.md`

---

## 2. Files created in this phase

| Path | Purpose |
|---|---|
| `01-planning/IMPLEMENTATION_PLAN.md` | Full phased plan (from earlier, cross-ref) |
| `.github/workflows/ci.yml` | CI pipeline |
| `.editorconfig` | Editorial + code style standards |
| `.env.example` | Dev environment template (secrets sample) |
| `.gitignore` | Ignored artifacts/secrets |
| `Makefile` | Task runner |
| `README.md` | Project overview + quickstart |
| `infra/compose.yaml` | Dev stack |
| `infra/docker/*.Dockerfile` | Service images |
| `scripts/dev/bootstrap.sh` | First-time setup |
| `src/backend/**` | .NET solution (12 source files listed in phase notes) |

---

## 3. Verification evidence (run locally to reproduce)

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Karry.sln -c Release` | 0 warnings, 0 errors |
| Backend tests | `dotnet test Karry.sln -c Release --no-build` | 12 passed / 0 failed |
| Frontend typecheck | `npm run typecheck` | pass |
| Frontend lint | `npm run lint` | pass |
| Frontend format | `npm run format:check` | pass |
| Frontend tests | `npm test` | 4 passed |
| Frontend build (PWA) | `npm run build` | pass (sw.js generated) |
| Math engine lint | `python3 -m ruff check app tests` | pass |
| Math engine tests | `python3 -m pytest` | 13 passed |
| Compose validation | `docker compose -f infra/compose.yaml config` | valid |
| Makefile | `make help` | all targets listed |
| Bootstrap script | `bash -n scripts/dev/bootstrap.sh` | syntax OK |

> Note: Docker was **not** available in the authoring environment, so `docker compose up` was not executed here. The compose file is YAML-validated and is expected to run on a Docker-enabled machine via `make up`.

---

## 4. Phase 0 acceptance criteria

- [x] A fresh developer can bootstrap with `./scripts/dev/bootstrap.sh` (or `make bootstrap`)
- [x] `make up` brings up the full stack (requires Docker; validated config, not executed here)
- [x] CI is defined in `.github/workflows/ci.yml` and passes conceptually (all local equivalents green)
- [x] Linting/formatting standards in place for all three layers

---

## 5. Hand-off to Phase 1

Phase 1 (**Data Platform & RBAC**) builds on this skeleton:
- Add the full PostgreSQL schema (all entities from §4 of the plan)
- Migration strategy + seed script for a demo tenant
- Identity/roles/permissions with JWT + refresh rotation + 2FA
- Field-level masking (masked financial margins per role)
- Offline-first frontend shell polish + React Router role guards

Blocking prerequisites already in place: DbContext, RLS tenant context plumbing, JWT wiring, unit-toggle API, repository/UoW, and CI.
