# Backend Architecture (.NET 9)

> **Layer:** `src/backend/` · **Extracted from** the cross-cutting [`../architecture.md`](../architecture.md).
> **Related:** [`02-domain.md`](02-domain.md) (domain model) · [`03-api.md`](03-api.md) (API contracts) · [`04-database.md`](04-database.md) (persistence) · [`../01-planning/IMPLEMENTATION_PLAN.md`](../01-planning/IMPLEMENTATION_PLAN.md)

---

## Overview

Clean/onion layering. **Dependency flow is strictly inward:**
`Api → Application → Domain` and `Api → Infrastructure → Application/Domain`.

### Projects & responsibilities

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

## Key architectural decisions

| Decision | Rationale |
|---|---|
| **DDD layers, inward-only deps** | Testability, separation of concerns; domain pure C# |
| **MediatR + FluentValidation** in Application | Decouples controllers from use cases; validation centralized |
| **EF Core + PostgreSQL/PostGIS** | Spatial blast grids; RLS; mature relational modeling |
| **Repository + UnitOfWork via `DbContext`** | `KarryDbContext` itself implements `IUnitOfWork`; generic `IRepository<T>` |
| **Tenant isolation** | `ICurrentTenant` resolved per request; domain entities implement `ITenantScoped`; DB writes stamped on add. (Full RLS enforcement: Phase 1) |
| **Math logic delegated to Python** | Numerical fidelity + NumPy for future DCG solvers; isolated, independently scaleable |
| **Value object for units (`Measure`)** | Encapsulates the dynamic unit toggle semantics in one testable place |

## DI composition

- `Karry.Application.DependencyInjection.AddApplication()` → MediatR + validators.
- `Karry.MathEngine.Client.DependencyInjection.AddMathEngineClient(config)` → `MathEngineOptions` + `HttpClient<KarryMathEngineClient>`.
- `Karry.Infrastructure.DependencyInjection.AddInfrastructure(config)` → `IHttpContextAccessor`, `TenantContext` (as both `ICurrentTenant` + `ICurrentUser`), `KarryDbContext`, `GenericRepository` for `IRepository<>`, `IUnitOfWork`, Redis.
- `Program.cs` wires everything, adds JWT bearer + Swagger, Serilog, and calls `MigrateDatabaseAsync` (auto-migrate on in Development).

## Style / contribution rules (backend)

- File-scoped namespaces, expression-bodied where single-line.
- `NoWarn=1591` (XML docs not required) + `TreatWarningsAsErrors=true`.
- Use the `Karry.Domains/Application/Infrastructure/Api` layering; never let outer layers leak into inner ones.
