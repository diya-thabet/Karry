# Karry Platform — Master Implementation Plan & Execution Phases

> **Karry** (*Application pour la Gestion de Carrière*) — Enterprise Quarry & Mining Management Operating System.
> This document translates the technical codex (`../02-reference/codex.tex`) into a complete, actionable, phase-by-phase engineering execution plan.

---

## Table of Contents

1. [Strategic Vision & Scope](#1-strategic-vision--scope)
2. [Architecture Blueprint](#2-architecture-blueprint)
3. [Repository & Monorepo Strategy](#3-repository--monorepo-strategy)
4. [Data Model & Database Schema Design](#4-data-model--database-schema-design)
5. [Core Domain Services](#5-core-domain-services)
6. [Execution Phases Overview](#6-execution-phases-overview)
7. [Phase 0 — Foundations & Tooling](#phase-0--foundations--tooling)
8. [Phase 1 — Data Platform & RBAC (Weeks 1–2)](#phase-1--data-platform--rbac-weeks-12)
9. [Phase 2 — Field PWA & Verification Pipeline (Weeks 3–4)](#phase-2--field-pwa--verification-pipeline-weeks-34)
10. [Phase 3 — Maintenance & Store Network (Weeks 5–6)](#phase-3--maintenance--store-network-weeks-56)
11. [Phase 4 — Analytics, Curves & AI Copilot (Weeks 7–8)](#phase-4--analytics-curves--ai-copilot-weeks-78)
12. [Phase 5 — Hardening, Security & Audit (Weeks 9–10)](#phase-5--hardening-security--audit-weeks-910)
13. [Phase 6 — Deployment, Pilot & Rollout (Weeks 11–12)](#phase-6--deployment-pilot--rollout-weeks-1112)
14. [Mathematical Engine Implementation Details](#14-mathematical-engine-implementation-details)
15. [Risk Register & Mitigations](#15-risk-register--mitigations)
16. [Definition of Done per Module](#16-definition-of-done-per-module)
17. [Testing Strategy](#17-testing-strategy)
18. [Team Structure & Responsibilities](#18-team-structure--responsibilities)
19. [Budget & Timeline Estimates](#19-budget--timeline-estimates)
20. [Acceptance Criteria & KPIs](#20-acceptance-criteria--kpis)

---

## 1. Strategic Vision & Scope

### 1.1 What Karry Solves

Karry is a **multi-tenant, offline-first SaaS** operational system for:
- Aggregate quarries (carrières)
- Sand washing plants
- Civil engineering extractions (open-pit)
- Heavy mobile fleet management

It digitizes the entire value chain from **blast bench → crusher plant → weighbridge → warehouse → shift controller → executive financial ledger**.

### 1.2 The 9 Industry Vulnerabilities Karry Eliminates

| # | Vulnerability | Karry Solution |
|---|---|---|
| 1 | Field data fraud & unverified fuel logging | Dual-meter logging, controller approval, fraud analytics |
| 2 | Fixed maintenance metric limitations | Hybrid metric tracking (Hours / KM / Calendar) with fallback |
| 3 | Conveyor belt speed degradation | Dynamic `Q_belt(t)` physics engine |
| 4 | Unpredicted wear component failures | RUL prediction + automated procurement triggers |
| 5 | Static vs closed-loop mass modeling | Directed Cyclic Graph (DCG) mass-balance engine |
| 6 | Paper bureaucracy & no digital signatures | Native PKI SHA-256 touchscreen e-signatures |
| 7 | Inter-quarry material leakage | Magasin Général hub + lifetime telemetry retention |
| 8 | Rigid load unit systems | Dynamic m³ ↔ Tonnes toggle |
| 9 | Unmonitored loan amortization vs weather | Solvency engine + weather-delay forecasting |

### 1.3 Non-Functional Requirements (NFRs)

- **Offline-first**: Full operation during network blackouts at remote sites; background sync on reconnect.
- **Multi-tenancy**: Strict tenant isolation via PostgreSQL Row-Level Security (RLS).
- **i18n**: English / French, multi-currency (USD, EUR, XOF, CAD), dual units (m³/Tonnes, Short/Metric tons).
- **Performance**: Sub-second dashboard loads on low-cost Android tablets; time-series curve rendering at 60fps.
- **Security**: PKI signatures, SHA-256 tamper-evident ledgers, RBAC with masked fields, immutable audit trails.
- **Availability**: Target 99.5% uptime; disaster recovery via PostgreSQL PITR (Point-In-Time Recovery).

---

## 2. Architecture Blueprint

### 2.1 Logical Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                                  │
│   React PWA (Field Tablets)  ·  Shift Controller UI  ·  Executive    │
│   Portal  ·  Touchscreen e-Signature Canvas  ·  Offline IndexedDB    │
└───────────────────────────────┬──────────────────────────────────────┘
                                │ HTTPS / REST / WebSocket
┌───────────────────────────────▼──────────────────────────────────────┐
│                       API GATEWAY (.NET 9)                           │
│   Auth (JWT + Refresh)  ·  RBAC Enforcer  ·  Rate Limiting           │
│   Versioning  ·  Request Correlation  ·  API Documentation (OpenAPI) │
└───────────────┬──────────────────────────────┬───────────────────────┘
                │                               │
┌───────────────▼───────────────────┐ ┌────────▼────────────────────────┐
│     CORE DOMAIN SERVICES (.NET 9) │ │   MATH & GRAPH ENGINE (Python)  │
│  - Identity & Tenancy             │ │   FastAPI + NumPy:             │
│  - Blast & Production             │ │   - Q_belt(t) conveyor physics │
│  - Equipment & Maintenance        │ │   - Closed-loop DCG solver      │
│  - Warehouse & Transfers          │ │   - RUL projection engine       │
│  - Shift Verification             │ │   - Weather/loan solver          │
│  - Financial Ledger               │ │                                 │
│  - PKI Signatures & Audit         │ └──────────────┬──────────────────┘
└───────────────┬───────────────────┘                │
                │ PostgreSQL 16 + PostGIS            │ (isolated calls)
┌───────────────▼────────────────────────────────────▼──────────────────┐
│                       DATA LAYER                                      │
│   PostgreSQL 16 · PostGIS (spatial blast grids) · RLS Policies        │
│   Redis Cache · RabbitMQ/In-Process Bus · S3/MinIO (documents)        │
└───────────────────────────────────────────────────────────────────────┘
```

### 2.2 Technology Decisions (with rationale)

| Layer | Tech | Rationale |
|---|---|---|
| Frontend PWA | React 18 + TypeScript + Tailwind CSS | Component reuse, TS safety, offline-first ecosystem |
| Client storage | Dexie.js (IndexedDB) | Offline shift queues, scale tickets, pending entries |
| Charts | Recharts + Canvas API | Interactive curves; Canvas for heavy time-series |
| Backend | .NET 9 Core (C#) | High-throughput REST, strong typing, EF Core 9 |
| Math engine | Python 3.12 + FastAPI + NumPy | Numerical fidelity, vectorization for graph solvers |
| Database | PostgreSQL 16 + PostGIS | RLS multi-tenancy, spatial blast grids, JSONB |
| Search/Cache | Redis | Session caching, hot telemetry, rate limiting |
| File storage | S3-compatible (MinIO/Cloudflare R2) | Waybills, signed docs, photo timestamps |
| AI layer | Semantic Kernel + OpenAI / local Llama | RAG copilot, anomaly detection, NL Q&A |
| Auth | ASP.NET Identity + JWT | Standardized, auditable auth |
| Infra | Docker Compose (dev) → Kubernetes (prod) | Reproducible, scalable deployment |
| Observability | OpenTelemetry + Prometheus + Grafana | Tracing, metrics, log correlation |

### 2.3 Cross-Cutting Concerns

- **Audit Logging**: Every mutation writes to an append-only `audit_log` (controller ID, original value, modified value, timestamp, IP, device, GPS).
- **Event Sourcing for Ledger**: Financial & stock movements use append-only event streams; current state is a projection.
- **Feature Flags**: `Karry.FeatureFlags` service enables staged rollouts (e.g., AI layer behind flag).
- **Idempotency Keys**: Field PWA sends `Idempotency-Key` header to prevent duplicate shift submissions during flaky network retries.
- **Tenant Context**: `app.current_tenant_id` set per request; every query constrained by RLS.

---

## 3. Repository & Monorepo Strategy

### 3.1 Recommended Repository Layout

```
karry/
├── docs/                          # All project documentation (this file, codex.tex)
├── .github/
│   ├── workflows/                 # CI/CD pipelines
│   ├── ISSUE_TEMPLATE/
│   └── PULL_REQUEST_TEMPLATE.md
├── src/
│   ├── backend/                   # .NET 9 solution
│   │   ├── Karry.Api/             # REST API host
│   │   ├── Karry.Application/     # Use cases, domain orchestration
│   │   ├── Karry.Domain/          # Entities, value objects, invariants
│   │   ├── Karry.Infrastructure/  # EF Core, Redis, S3, integration adapters
│   │   ├── Karry.MathEngine.Client/ # Typed HTTP client to Python engine
│   │   └── Karry.Tests/           # Unit + integration tests
│   ├── math-engine/               # Python FastAPI service
│   │   ├── app/
│   │   │   ├── main.py
│   │   │   ├── api/
│   │   │   ├── core/              # Q_belt, DCG solver, RUL
│   │   │   └── schemas/
│   │   └── tests/
│   ├── frontend/                  # React PWA
│   │   ├── src/
│   │   │   ├── app/               # routes, providers
│   │   │   ├── features/          # per-module feature folders
│   │   │   ├── components/        # shared UI
│   │   │   ├── hooks/
│   │   │   ├── lib/               # api client, offline queue
│   │   │   └── stores/            # state management (Zustand)
│   │   └── public/                # PWA manifests, service worker
│   └── shared/                    # OpenAPI specs, shared types, protobuf
├── infra/
│   ├── docker/                    # Dockerfiles
│   ├── k8s/                       # Helm charts / manifests
│   ├── terraform/                 # Cloud provisioning
│   └── compose.yaml               # Local dev stack
├── scripts/
│   ├── db/                        # Migrations, seeders, backups
│   └── dev/                       # Dev bootstrap scripts
└── .env.example
```

### 3.2 Branching Strategy

- `main` — always deployable, protected (PR required, CI green).
- `develop` — integration branch.
- `feature/<area>/<ticket-id>-slug` — e.g. `feature/maintenance/rul-engine-124`.
- `release/vX.Y.Z` — release candidates.
- `hotfix/<ticket>` — critical fixes.

Commit convention: [Conventional Commits](https://www.conventionalcommits.org) — `feat:`, `fix:`, `chore:`, `refactor:`, `docs:`, `test:`.

---

## 4. Data Model & Database Schema Design

### 4.1 Core Entities (PostgreSQL 16)

**Identity & Tenancy**
- `tenants` (id, name, country, currency, timezone, locale)
- `users` (id, tenant_id, email, password_hash, name, role_id, device_ids)
- `roles` (id, tenant_id, name)
- `permissions` (id, tenant_id, resource, action: read/write/mask)
- `role_permissions` (join table)
- `refresh_tokens`, `audit_log`

**Site & Blast**
- `sites` (id, tenant_id, name, gps_location geography, type)
- `blast_patterns` (id, site_id, area_m2, bench_height_m, slope_deg, geometry)
- `blasts` (id, site_id, pattern_id, V_rock_m3, rho_rock, kappa_moisture, M_raw_t)
- `production_runs` (id, site_id, shift_id, date)

**Equipment & Machines**
- `machines` (id, tenant_id, site_id, name, type, model, serial)
- `machine_nodes` (id, machine_id, graph_type: crusher/screen/loader/conveyor/hauler, upstream_node_ids[], downstream_node_ids[])
- `wear_parts` (id, machine_id, name, category, rating_unit: hours/km/days, U_rating, M_rating, bond_abrasion_index)
- `wear_usage_logs` (id, wear_part_id, delta_h, delta_km, tonnage_processed, date)
- `maintenance_events` (id, machine_id, wear_part_id, type, cost, date, source)
- `maintenance_schedule` (derived RUL projections)

**Shifts & Verification**
- `shifts` (id, site_id, machine_id, operator_id, controller_id, type: day/night, start_time, end_time)
- `shift_entries` (id, shift_id, meter_start, meter_end, delta_h, fuel_start, fuel_end, photo_timestamp, status: pending/approved/rejected/modified)
- `entry_audit_overrides` (id, entry_id, controller_id, original_value, modified_value, reason, timestamp)
- `scale_tickets` (id, site_id, weighbridge_id, ticket_no, truck_reg, net_weight_t, volume_m3, unit, signature_id, status)

**Warehouse & Logistics**
- `warehouses` (id, tenant_id, type: central/site_store/workshop, name)
- `inventory_items` (id, warehouse_id, sku, name, category, qty, unit, cost_price)
- `stock_movements` (id, item_id, type: receipt/issue/transfer, qty, ref_waybill_id, timestamp)
- `transfer_waybills` (id, from_warehouse_id, to_warehouse_id, asset_carryover_json, status, signature_id)
- `asset_transfers` (id, machine_id, from_warehouse_id, to_warehouse_id, lifetime_metrics_json, date)
- `physical_inventory_audits` (id, warehouse_id, audit_date, initial_stock, receipts, issues, physical_stock, variance, flags)

**Financial**
- `ledger_entries` (id, tenant_id, entity_type, entity_id, account, amount, currency, event_payload, created_at)
- `loans` (id, machine_id, principal, interest_rate, term_months, start_date)
- `amortization_schedules` (id, loan_id, period, payment, principal_portion, interest_portion)
- `operational_costs` (id, machine_id, date, capital_cost, residual_value, lifespan_hours, fuel_conso, fuel_price, operator_wage, maintenance_cost)

**PKI & Signatures**
- `signatures` (id, tenant_id, signer_user_id, payload_sha256, vector_data, gps, device_info, created_at, certificate_json)
- `signed_documents` (id, document_type, document_id, signature_id, document_hash)

**Weather & Analytics**
- `weather_forecasts` (id, site_id, date, rain_stoppage_days_expected)
- `production_telemetry` (id, site_id, machine_id, timestamp, q_belt, m_dot, belt_speed, tonnage)

### 4.2 Schema Design Principles

1. **Tenant isolation**: every table has `tenant_id`; RLS policy:
   ```sql
   CREATE POLICY tenant_isolation_policy ON table_name
   USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
   ```
2. **Append-only audit**: `audit_log` rows never mutated/deleted.
3. **JSONB for extensibility**: machine graph edges, asset carryover files, signature vectors.
4. **Spatial**: PostGIS `geography` for blast patterns, site GPS, geofencing.
5. **Soft delete** for reference data; hard delete only for admin-purged tenants.
6. **Composite indexes** on high-traffic queries: `(tenant_id, site_id, date)`, `(tenant_id, machine_id, timestamp)`.

### 4.3 Migration Strategy

- EF Core migrations for .NET domain model.
- Python engine uses dedicated migration tool (Alembic) if it owns tables; ideally the math engine stays stateless and reads via API — no direct DB access (single source of truth through API).
- Migrations run in CI with `dotnet ef database update` gated by automated rollback test.

---

## 5. Core Domain Services

Each service is a bounded context with its own API surface, invariants, and data.

| Service | Key Responsibilities | Critical Invariants |
|---|---|---|
| **Identity & Tenancy** | Login, JWT, refresh, roles, permissions | One active session token; masked fields per role |
| **Blast & Production** | Volume math, mass engine, dynamic unit toggle | `M_raw = V × ρ × κ`; conversion reversible |
| **Equipment Registry** | Machine graph nodes, plug-and-play topology | No orphan nodes; cycles validated at design time |
| **Maintenance RUL** | Hybrid metrics, fallback switch, procurement triggers | `RUL <= lead_time + buffer` → trigger; single active metric |
| **Shift Verification** | Dual-shift continuity, controller approvals | `H_start,night ≡ H_end,day` else fraud flag |
| **Warehouse** | Stock ledger, transfers, audits | `Stock >= 0`; variance auto-flags |
| **Financial Ledger** | Costs, loans, solvency | Append-only; solvency check per period |
| **PKI & Signatures** | Capture, hash, certify | Signature binds payload SHA-256 + GPS + identity |
| **AI Copilot** | RAG, anomaly, NL Q&A | Prompt sanitization; PII masking; per-role visibility |
| **Analytics** | Curves, heatmaps, projections | Consistent time bucketing; cache invalidation |

---

## 6. Execution Phases Overview

```
Phase 0: Foundations & Tooling            (3 days)
Phase 1: Data Platform & RBAC             (Weeks 1–2)
Phase 2: Field PWA & Verification         (Weeks 3–4)
Phase 3: Maintenance & Store Network      (Weeks 5–6)
Phase 4: Analytics, Curves & AI           (Weeks 7–8)
Phase 5: Hardening, Security & Audit      (Weeks 9–10)
Phase 6: Deployment, Pilot & Rollout      (Weeks 11–12)
```

Each phase ends with a **demo-able milestone**, deployable to staging.

---

## Phase 0 — Foundations & Tooling

**Goal**: Reproducible dev environment, CI/CD skeleton, coding standards.

### Deliverables
- [x] Git repo initialized with branch protection
- [ ] Monorepo scaffolding per §3.1
- [ ] Docker Compose dev stack: Postgres+PostGIS, Redis, MinIO, backend, math-engine, frontend
- [ ] CI pipeline (GitHub Actions): build, test, lint, typecheck, docker build
- [ ] Linting/formatting configs: ESLint+Prettier (FE), .editorconfig+StyleCop (BE), Ruff+Black (Python)
- [ ] `.env.example`, secret management baseline (SOPS/vault)
- [ ] Makefile / task runner with common commands (`make up`, `make test`, `make migrate`)

### Acceptance
- A fresh developer can run `make bootstrap && make up` and hit all services in < 10 minutes.
- CI passes on `main` with zero manual steps.

---

## Phase 1 — Data Platform & RBAC (Weeks 1–2)

**Goal**: Solid data foundation + secure multi-tenant auth and role model.

### 1.1 Tasks — Backend Foundation
- [ ] Scaffold .NET 9 solution with Clean Architecture layering.
- [ ] PostgreSQL connection, EF Core setup, initial migrations for all core entities (§4.1).
- [ ] RLS policies + tenant context middleware (`app.current_tenant_id` per request).
- [ ] Seed script: demo tenant, roles, admin user.

### 1.2 Tasks — Identity & RBAC
- [ ] Registration/login (password hashing with ASP.NET Identity).
- [ ] JWT access tokens + refresh token rotation, device binding.
- [ ] Roles & permissions matrix (admin, controller, operator, weighmaster, storekeeper, executive).
- [ ] Field-level masking (e.g., executives hide cost margins from operators).
- [ ] Password policy + account lockout; 2FA (TOTP) optional for admin.

### 1.3 Tasks — Dynamic Unit Toggle API
- [ ] `/api/units/convert` endpoint: m³↔Tonnes (moisture-adjusted), Short↔Metric tons.
- [ ] Per-tenant default units with per-user override; audit conversion usage.

### 1.4 Tasks — Frontend Shell
- [ ] React app bootstrapped with Tailwind; routing, providers (auth, tenant).
- [ ] Login screen + role-based routing guards.
- [ ] Global API client with auth interceptors + idempotency-key support.
- [ ] PWA manifest + service worker skeleton (offline shell).

### Milestone 1 Demo
Admin creates a tenant, two roles, and an operator; operator logs in; unit conversion works both ways.

---

## Phase 2 — Field PWA & Verification Pipeline (Weeks 3–4)

**Goal**: Digitize shift logging, two-tier approval, e-signatures, weighbridge tickets — all offline-capable.

### 2.1 Tasks — Shift Entry PWA
- [ ] Machine/shift selection UI with camera photo timestamps (EXIF/GPS).
- [ ] Shift start/end meter logging: `H_start`, `H_end`, fuel gauge, `ΔH = H_end − H_start`.
- [ ] **Dual-shift continuity validation**: reject if `H_start,night ≠ H_end,day`; auto flag.
- [ ] Offline queue (Dexie) with background sync; optimistic UI + conflict resolution.

### 2.2 Tasks — Controller Approval Pipeline
- [ ] Pending queue for controllers; approve / reject / modify actions.
- [ ] Audit override trail: controller ID, original, modified, timestamp, reason.
- [ ] Fraud-flag dashboard feed (mismatches, abnormal `ΔH`).

### 2.3 Tasks — PKI Electronic Signatures
- [ ] Canvas signature capture component (vector data).
- [ ] SHA-256 hash of payload; bundle with credentials, GPS, device info → certificate JSON.
- [ ] Signature verification endpoint (`POST /api/signatures/verify`).
- [ ] Apply to: shift handovers, weighbridge tickets, transfer waybills, approvals.

### 2.4 Tasks — Weighbridge Tickets
- [ ] Digital scale ticket creation (net weight, truck registration, timestamp, signature).
- [ ] Tamper-evident: no manual edit; corrections via counter-signed reversal entries.
- [ ] Offline ticket queue + sync.

### Milestone 2 Demo
Simulate a full day: night-shift handover logs (continuity enforced), controller approves with e-signature, weighbridge issues ticket, all simulated offline then synced.

---

## Phase 3 — Maintenance & Store Network (Weeks 5–6)

**Goal**: Hybrid RUL engine, Magasin Général multi-warehouse logistics, inventory audits.

### 3.1 Tasks — Hybrid Maintenance Engine
- [ ] Wear parts registry with rating units (Hours / KM / Days).
- [ ] Usage logging (`ΔH`, `ΔK`, tonnage processed) per shift.
- [ ] **RUL computation** (delegated to math engine):
  ```text
  RUL_m(p) = min( (U_rating − U_accumulated) / (ū_daily × δ_abrasion),
                  (M_rating − M_processed) / (m̄_daily × δ_abrasion) )
  ```
- [ ] **Meter Breakdown Fallback Switch**: authorized manager toggles metric; system auto-computes calendar estimate from 30-day average usage.
- [ ] **Automated Procurement Trigger**: when `RUL_days ≤ lead_time + τ_buffer` → procurement alert + PO draft.
- [ ] Maintenance event scheduling grouped to minimize plant stops (Phase 4 AI enhancement).

### 3.2 Tasks — Multi-Warehouse Logistics
- [ ] Warehouse registry (central / site store / workshop).
- [ ] Stock ledger (receipts, issues) with real-time balances.
- [ ] **Transfer waybills** with metric carryover JSON (accumulated hours/KM, maintenance history, fuel averages, depreciation).
- [ ] **Lifetime telemetry retention**: asset keeps unified timeline across all warehouses (no resets).
- [ ] Transfer audit trail with e-signature.

### 3.3 Tasks — Physical Inventory Audits
- [ ] Semi-annual/annual audit workflow:
  ```text
  ΔStock = Stock_physical − (Stock_initial + Receipts − Issues)
  ```
- [ ] Auto-flag any `ΔStock ≠ 0`; assign responsible store officer; escalation to admin.

### Milestone 3 Demo
Transfer a jaw-liner set from Central Warehouse to Site B with full carryover; RUL triggers a procurement alert before lead-time; run an inventory audit that flags a discrepancy.

---

## Phase 4 — Analytics, Curves & AI Copilot (Weeks 7–8)

**Goal**: Interactive dashboards, conveyor/loan math visualization, executive AI layer.

### 4.1 Tasks — Math Engine Integration (FastAPI)
- [ ] `POST /engine/conveyor`: compute `Q_belt(t)` with wear/inclination/weather factors.
- [ ] `POST /engine/massbalance`: solve closed-loop DCG for screens & crushers (circulating load).
- [ ] `POST /engine/rul`: project RUL vectors across fleets.
- [ ] `POST /engine/solvency`: loan amortization vs net operating revenue + weather risk.
- [ ] Unit tests against hand-computed fixtures.

### 4.2 Tasks — Visual Analytics
- [ ] **Production & conveyor curves**: `Q_belt(t)`, `ṁ(t)` time-series (Recharts line).
- [ ] **Wear decay trajectories**: RUL trendlines with `τ_buffer` threshold markers.
- [ ] **Fuel efficiency scatter**: fuel vs tonnage with +18% anomaly highlight.
- [ ] **Loan amortization lines**: net revenue vs financing curve, rain-disruption overlays.
- [ ] Role-tailored dashboards (tablet / controller / executive).

### 4.3 Tasks — AI Copilot
- [ ] Telemetry/ledger context assembler + prompt sanitizer (PII masking).
- [ ] RAG index over operational docs + live telemetry (Semantic Kernel / OpenAI / Llama).
- [ ] NL Q&A (EN/FR): *“Did Loader CAT-966 cover its monthly bank loan?”*, *“Which warehouse transferred jaw plates to Site B?”*
- [ ] Automated fuel theft detection: `ΔH/ΔK` vs fuel vs output; `+18%` spike → alert.
- [ ] Conveyor bottleneck alerts on low `Q_belt(t)`.
- [ ] Weather sales-advisory (rain `R(m)` → delivery delay warnings).

### Milestone 4 Demo
Executive asks a French-language question; gets an answer with source trace; dashboard shows live conveyor curve with bottleneck flagged; loan line shows under-water asset.

---

## Phase 5 — Hardening, Security & Audit (Weeks 9–10)

**Goal**: Production-grade security, performance, and reliability.

### Tasks
- [ ] Full RBAC review + field-masking regression suite.
- [ ] Penetration-test pass: JWT misuse, IDOR, SQL injection, XSS, SSRF.
- [ ] Rate limiting & brute-force protection; audit log completeness (append-only verified).
- [ ] Signature certificate verification end-to-end test.
- [ ] Load testing (k6): dashboards under 500 concurrent tablets; math engine latency < 500ms p95.
- [ ] Caching strategy (Redis) for telemetry aggregation.
- [ ] Backups: PITR enabled; restore drill documented and rehearsed.
- [ ] Observability: OpenTelemetry tracing, structured logs, Grafana dashboards, error alerting.

### Milestone 5 Demo
Security test report green; load test at target concurrency; restore drill successful.

---

## Phase 6 — Deployment, Pilot & Rollout (Weeks 11–12)

**Goal**: Ship to production, train users, pilot at one site, then scale.

### Tasks
- [ ] Kubernetes manifests + Helm charts; staged deployment (canary).
- [ ] Environment promotion: dev → staging → prod; DB migrations automated.
- [ ] Multi-currency & i18n final pass (EN/FR, XOF/USD/EUR/CAD).
- [ ] Field training materials + controller playbook (FR).
- [ ] Pilot at one quarry (2-week sprint): operators + controllers + storekeepers.
- [ ] Feedback loop: bug triage, UX fixes, performance tuning.
- [ ] Rollout plan: sequential site onboarding, each with data migration from paper/excel.
- [ ] Go-live checklist + runbook + on-call rotation.

### Milestone 6 Demo
Production running; pilot site reporting live data; exec dashboard populated; sign-off from pilot controller.

---

## 14. Mathematical Engine Implementation Details

### 14.1 Blast Volume & Mass
```text
V_rock = A_pattern × h_bench × cos(θ_slope)
M_raw  = V_rock × ρ_rock × κ_moisture
η_yield = (Σ M_product_i / M_raw) × 100%
```
- Store `A_pattern`, `h_bench`, `θ_slope` from PostGIS geometry.
- `κ_moisture ≥ 1.0` defaults per weather forecast; editable by controller.

### 14.2 Conveyor Physics
```text
Q_belt(t) = Q_nominal × φ_wear(H_belt) × ψ_inclination(θ) × ω_weather(R)
```
- `φ_wear`: derived from belt age/tonnage decay curve (configurable coefficients).
- `ψ_inclination`: lookup table by angle (calibrated per site).
- `ω_weather`: rainfall threshold table (e.g., >50mm/day → 0.6).

### 14.3 Closed-Loop Mass Balance (DCG Solver)
```text
ṁ_Fj(t) = ṁ_primary(t) + Σ_{k∈Recirc} γ_k · ṁ_Ck(t−1)
```
- Iterative fixed-point solver (NumPy) until mass converges (<0.1% tolerance).
- Graph topology from `machine_nodes`; plug-and-play recompute on topology change.

### 14.4 RUL Engine
```text
RUL_m(p) = min( (U_rating − U_accum) / (ū_daily × δ_abrasion),
                (M_rating − M_processed) / (m̄_daily × δ_abrasion) )
trigger when RUL_days ≤ T_shipping_lead_time + τ_buffer
```
- `δ_abrasion` from Bond abrasion index; per-wear-part config.
- Fallback: 30-day rolling average `ū_daily` when meters break.

### 14.5 Fleet & Solvency
```text
C_ops(a) = (C_capital − S_residual)/H_lifespan + (F_conso × P_fuel) + W_operator + M_maintenance
Σ (Revenue_a(d) − C_ops(a)×ΔH(d)) > C_loan_annual / T   → solvent
P_eff(m) = P_nominal × (1 − (R(m) + σ_safety)/N_total)
T_delivery = Q_order / P_eff(m)
```

---

## 15. Risk Register & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Math engine inaccuracy vs real site | Med | High | Calibration coefficients per site; fixture tests; pilot validation |
| Offline sync conflicts | Med | High | Idempotency keys, server-as-source-of-truth, conflict UI for controllers |
| RLS misconfiguration leaks tenant data | Low | Critical | Mandatory RLS test suite; DBAs review; masked-field penetration tests |
| Field user adoption resistance | High | Med | Paper-parallel pilot, FR training, ultra-simple PWA UX |
| Signature repudiation | Low | High | Bind GPS+device+SHA-256; time authority; certificate verification tool |
| Weather data provider outages | Med | Med | Cached forecasts, manual override, graceful degradation |
| AI copilot hallucination | Med | Med | RAG grounded in telemetry only; source citations; human-in-loop for alerts |
| Load spike during sync (end of shift) | Med | Med | Backoff+exponential retry, throttled sync, queue-based ingestion |

---

## 16. Definition of Done per Module

A module is **done** when:
- [ ] Code reviewed + merged with passing CI.
- [ ] Unit + integration tests ≥ 80% coverage on critical paths.
- [ ] Offline behavior tested (devtools throttling, airplane mode sim).
- [ ] RBAC/masking verified by role.
- [ ] Audit logging verified (mutation → audit row).
- [ ] i18n (EN/FR) strings externalized.
- [ ] OpenAPI updated; frontend types regenerated.
- [ ] Documented in `docs/` (API contract, runbook if infra-affecting).

---

## 17. Testing Strategy

| Level | Tool | Scope |
|---|---|---|
| Unit (BE) | xUnit + FluentAssertions | Domain invariants, RUL math, conversion math |
| Unit (Python) | pytest | Conveyor physics, DCG solver, solvency |
| Unit (FE) | Vitest + Testing Library | Components, unit toggle, offline queue |
| Integration | Testcontainers (Postgres) | RLS, approval pipeline, transfers, audit |
| E2E | Playwright | Full shift flow, signature capture, dashboard |
| Load | k6 | Telemetry ingestion, dashboards, math engine |
| Security | OWASP ZAP + manual | Auth, RBAC, XSS, SSRF, injection |

---

## 18. Team Structure & Responsibilities

| Role | Focus |
|---|---|
| Backend .NET Engineer (×2) | Domain services, RLS, APIs, audit |
| Frontend PWA Engineer (×2) | React, offline, e-signature canvas, dashboards |
| Math/Data Engineer (×1) | FastAPI engine, DCG solver, RUL, solvency |
| DevOps Engineer (×0.5) | CI/CD, k8s, observability, backups |
| QA Engineer (×1) | Test strategy, E2E, load, security regression |
| Product/Domain SME (×1) | Quarry domain rules, pilot coordination, FR content |

---

## 19. Budget & Timeline Estimates

- **Timeline**: 12 weeks (≈ 3 months) to production pilot.
- **Effort**: ~7 FTE-equivalents across the window (~85 person-weeks).
- **Infra cost (est.)**: ~$300–600/mo at pilot scale (2 sites, 100 devices) — adjust per hosting region.

---

## 20. Acceptance Criteria & KPIs

### Acceptance (Go-Live)
- [ ] Pilot site operates 100% digitally for 2 consecutive weeks (no paper fallback).
- [ ] Zero unresolved security findings above MEDIUM.
- [ ] All 9 codex vulnerability solutions demonstrably live (§1.2).
- [ ] P99 API latency < 1.5s on field devices; offline sync 100% lossless.

### KPIs
| KPI | Target |
|---|---|
| Field entry processing time | −60% vs paper |
| Fuel variance detected | >90% of real theft cases in pilot |
| Unplanned downtime from wear | −40% within 3 months |
| Shift approval cycle | < 24h, mostly same-day |
| Inventory shrinkage flagged | 100% variance detection |
| Tenant isolation test pass | 100% |

---

*End of implementation plan. This document is a living artifact — update it as the build progresses and lessons are learned. All cross-references point to `../02-reference/codex.tex` for the canonical domain specification.*