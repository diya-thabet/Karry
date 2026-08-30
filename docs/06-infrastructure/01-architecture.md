# Infrastructure — Tooling, Deployment & CI

> **Layer:** `infra/`, `.github/`, `scripts/`, `Makefile` · **Extracted from** the cross-cutting [`../architecture.md`](../architecture.md).

---

## Local dev stack (`infra/compose.yaml`)

| Service | Image | Notes |
|---|---|---|
| `postgres` | `postgis/postgis:16-3.4` | Postgres + PostGIS; healthcheck `pg_isready` |
| `redis` | `redis:7-alpine` | Cache; healthcheck `redis-cli ping` |
| `minio` | `minio/minio` | Objects (sw :9000, console :9001) |
| `math-engine` | build from `infra/docker/math-engine.Dockerfile` | healthcheck `/health` |
| `backend` | build from `infra/docker/backend.Dockerfile` | depends_on healthy postgres/redis/math |
| `frontend` | build from `infra/docker/frontend.Dockerfile` | vite dev :5173 |

Named volumes: `postgres_data`, `minio_data`.

## Dockerfiles (`infra/docker/`)

- `backend.Dockerfile` — multi-stage .NET 9: restore → publish → `aspnet:9.0` runtime.
- `math-engine.Dockerfile` — multi-stage Python 3.12: pip install → slim runtime → uvicorn.
- `frontend.Dockerfile` — build + dev stages; `npm ci` then `npm run build` / `npm run dev`.
- Each build context has its own `.dockerignore` (excludes bin/obj, node_modules, dist, .venv).

## CI/CD (`.github/workflows/ci.yml`)

Runs on push/PR to `main` / `develop` — four jobs:
- **backend** — restore → build → test (uploads TRX results)
- **frontend** — lint → format:check → typecheck → test → build (PWA)
- **math-engine** — pip install -e '.[dev]' → ruff → pytest
- **docker-images** — **on pushes only** (skipped on PRs): builds all three service images (Buildx + GHA cache) and **pushes to Docker Hub** (`dhiathabet/karry-*`, tagged `latest` + commit SHA). Requires the `DOCKERHUB_USERNAME` / `DOCKERHUB_TOKEN` repo secrets.

## Local utility

- `scripts/dev/bootstrap.sh` — copies env templates, installs frontend deps, restores backend, builds images.
- `Makefile` — `make up|down|build|logs|ps|validate-compose`, `make test*`, `make lint|typecheck`, `make math-*|backend-run|frontend-run`, `make migrate|clean`.

## Environment variables

| Variable | Purpose |
|---|---|
| `POSTGRES_USER/PASSWORD/DB` | Database credentials |
| `MINIO_ROOT_USER/PASSWORD` | Object storage credentials |
| `JWT_SECRET` | JWT signing secret (dev-only default) |
| `ConnectionStrings__KarryDatabase` | Overrides DB connection for backend container |

Secrets are excluded via `.gitignore`; only `.env.example` templates are committed.

## URLs (local dev)

| Service | URL |
|---|---|
| Frontend (PWA) | http://localhost:5173 |
| Backend (Swagger) | http://localhost:5000/swagger |
| Math Engine | http://localhost:8000/docs |
| MinIO console | http://localhost:9001 |
