# Karry — Documentation Index

Layer-based docs structure. **New docs:** put them in the matching layer folder below; update this index.

| Folder | Layer | What you'll find here |
|---|---|---|
| [`00-guidelines/`](00-guidelines/) | Guidelines | Guides for contributors & LLMs — read [`01-docs-structure.md`](00-guidelines/01-docs-structure.md) first |
| [`01-planning/`](01-planning/) | Planning | Phased build plans, project roadmap |
| [`02-reference/`](02-reference/) | Reference | Canonical domain & math specification |
| [`03-backend/`](03-backend/) | Backend | .NET services: architecture, domain, API, database |
| [`04-frontend/`](04-frontend/) | Frontend | React PWA: architecture, patterns |
| [`05-math-engine/`](05-math-engine/) | Math Engine | Python FastAPI service: architecture, contracts |
| [`06-infrastructure/`](06-infrastructure/) | Infrastructure | Docker, CI/CD, tooling, local dev |
| [`07-execution/`](07-execution/) | Execution | Phase logs (per completed phase) |
| [`08-knowledge/`](08-knowledge/) | Knowledge | Lessons learned (LLM-handsoff), troubleshooting |
| [`architecture.md`](architecture.md) | — | Cross-cutting system overview (all layers) |

## Search by subject

| Subject | Where to look |
|---|---|
| Where to put a new doc (for LLMs/agents) | `00-guidelines/01-docs-structure.md` |
| System overview / how layers fit | `architecture.md` |
| Build plan / phases / roadmap | `01-planning/IMPLEMENTATION_PLAN.md` |
| Domain formulas, units, DCG math, codex | `02-reference/codex.tex` |
| .NET projects, layering, order | `03-backend/01-architecture.md` |
| Domain entities, RUL, Measure, Machine | `03-backend/02-domain.md` |
| Endpoints, DTOs, Swagger | `03-backend/03-api.md` |
| Postgres, EF Core, RLS, migrations | `03-backend/04-database.md` |
| React structure, PWA, proxies | `04-frontend/01-architecture.md` |
| Conveyor physics, RUL, engine contracts | `05-math-engine/01-architecture.md` |
| Compose, Dockerfiles, CI jobs, env vars | `06-infrastructure/01-architecture.md` |
| What was done in a phase | `07-execution/phaseN-*.md` (`phase0-foundations-tooling.md`, `phase1-data-platform-rbac.md`) |
| Known gotchas / hard-won fixes | `08-knowledge/llmhandsoff.md` |

## Conventions

- **Every phase** → write a log in `07-execution/phaseN-<slug>.md`.
- **Every non-obvious fix/how-to** → append to `08-knowledge/llmhandsoff.md`.
- **Architecture changes** → update the relevant layer doc (and `architecture.md` if cross-cutting).