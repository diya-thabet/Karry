# Karry Platform

**Enterprise Quarry & Mining Management Operating System** (_Application pour la Gestion de Carrière_).

Karry is a multi-tenant, offline-first SaaS for aggregate quarries, sand washing plants, and open-pit mining operations. It ties field extraction → crusher plant → weighbridge → warehouses → shift controllers → executive financial ledgers into a tamper-proof intelligence system.

See [`docs/codex.tex`](docs/codex.tex) for the canonical domain specification and [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md) for the phased build plan.

---

## Repository Layout

```
├── docs/                        # Design docs (codex.tex, IMPLEMENTATION_PLAN.md)
├── .github/workflows/ci.yml     # CI pipeline
├── infra/
│   ├── compose.yaml             # Local dev stack
│   ├── docker/                  # Service Dockerfiles
│   └── terraform/               # (future) cloud provisioning
├── scripts/
│   ├── dev/bootstrap.sh         # First-time setup
│   └── db/                      # (future) migrations & seeds
└── src/
    ├── backend/                 # .NET 9 (REST API, domain, infrastructure)
    ├── math-engine/             # Python FastAPI + NumPy (conveyor physics, RUL)
    └── frontend/                # React 18 + TS + Tailwind PWA
```

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend PWA | React 18, TypeScript, Vite, Tailwind CSS, Dexie (offline) |
| Backend API | .NET 9 (ASP.NET Core, EF Core), PostgreSQL 16 + PostGIS |
| Math Engine | Python, FastAPI, NumPy |
| Supporting | Redis, MinIO, Docker Compose, GitHub Actions |

---

## Quickstart (Development)

Prerequisites: **Docker**, **Node 22**, **.NET SDK 9**, **Python 3.12+**.

```bash
# 1. One-time bootstrap (env templates + deps + images)
./scripts/dev/bootstrap.sh
#   —or—
make bootstrap

# 2. Start the full stack (Postgres+PostGIS, Redis, MinIO, API, math engine, frontend)
make up
```

| Service | URL |
|---|---|
| Frontend (PWA) | http://localhost:5173 |
| Backend API (Swagger) | http://localhost:5000/swagger |
| Math Engine | http://localhost:8000/docs |
| MinIO Console | http://localhost:9001 |

### Local (non-Docker) development

```bash
make math-install   # install python deps
make math-run       # uvicorn on :8000
make backend-run    # dotnet run on :5000
make frontend-run   # vite dev on :5173
```

> You still need Postgres/Redis locally (or via `docker compose up postgres redis minio`).

---

## Validation Targets (Makefile)

```bash
make test      # all test suites
make lint      # lint across projects
make typecheck # TypeScript typecheck
make migrate   # apply EF Core migrations
```

---

## CI

`.github/workflows/ci.yml` runs on push/PR to `main`/`develop`:

- **Backend**: restore → build → test (xUnit)
- **Frontend**: lint → format check → typecheck → test (Vitest) → build (PWA)
- **Math Engine**: lint (Ruff) → test (pytest)
- **Docker images**: build all three service images

---

## Configuration

Copy `.env.example` → `.env` for the compose stack, and `src/frontend/.env.example` → `src/frontend/.env.local` for the PWA.

| Variable | Purpose |
|---|---|
| `POSTGRES_*` | Database credentials |
| `MINIO_*` | Object storage credentials |
| `JWT_SECRET` | JWT signing secret (dev-only default) |

> **Never commit real secrets.** The `.gitignore` excludes `.env` files and key material.

---

## Status

Phase 0 — **Foundations & Tooling** — scaffolded:

- [x] Monorepo structure
- [x] .NET 9 backend skeleton (Clean Architecture: Domain / Application / Infrastructure / Api / MathEngine.Client / Tests)
- [x] Python FastAPI math engine (conveyor physics + RUL + endpoints)
- [x] React 18 + TS + Tailwind + Vite PWA (offline manifest, unit toggle)
- [x] Docker Compose dev stack
- [x] CI pipeline
- [x] Linting/formatting configs
- [x] `.env.example`, `.gitignore`
- [x] Makefile task runner

Next: **Phase 1 — Data Platform & RBAC** (see `docs/IMPLEMENTATION_PLAN.md`).
