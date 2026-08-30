# Backend — Domain Model

> **Layer:** `src/backend/Karry.Domain/` · **Related:** [`01-architecture.md`](01-architecture.md) · [`04-database.md`](04-database.md)
> **Source of truth for domain math:** [`../02-reference/codex.tex`](../02-reference/codex.tex)

---

## Common abstractions (`Karry.Domain/Common/`)

| Type | Purpose |
|---|---|
| `BaseEntity` | `Id`, `CreatedAtUtc`, `UpdatedAtUtc`, `MarkUpdated()` |
| `ValueObject` | Structural equality base for value types |
| `IAuditableEntity` | `CreatedBy` / `ModifiedBy` for audit attribution |
| `ITenantScoped` | `SetTenantId(Guid)` — DB stamps tenant on add |
| `ICurrentTenant` / `ICurrentUser` | Resolve ambient tenant/user for a request |
| `IRepository<T>` / `IUnitOfWork` | Persistence ports (implemented by Infrastructure) |

## Core aggregates

### `Tenant` (`Tenants/Tenant.cs`)
Tenant profile: `Name`, `Country`, `Currency`, `Timezone`, `Locale`. Created/updated via factories with validation.

### `Measure` (`Units/Measure.cs`) — dynamic unit toggle
Value object enabling the **m³ ↔ Tonnes** conversion:
```
M = V × ρ × κ_moisture          (volume → mass, moisture-adjusted density)
V = M / (ρ × κ_moisture)        (mass → volume)
```
- Supports Metric Ton and Short Ton; encapsulates the conversion factor in one constant (`0.90718474` → 1 st = 0.90718474 t).
- Round-trip convertible; rejects non-positive density or `κ < 1.0`.

### `Machine` (`Equipment/Machine.cs`) — graph node engine
The codex "Graph Node Engine" (ℳ_e): type, tracked wear parts, downstream routing edges (`E_out`).
- `ConnectTo(...)` / `DisconnectFrom(...)` manage the plant directed-graph topology.
- `RecordUsage(...)` accumulates hours/kilometers.

### `WearPart` (`Maintenance/WearPart.cs`) — hybrid maintenance
Tracks wear components (jaw liner, screen mesh, etc.) under a switchable `MeterKind` (Hours / Kilometers / CalendarDays).
- `RecordUsage(...)` accumulates hours, km, tonnage.
- `ComputeRemaining(dailyUsage, dailyTonnage)` → **RUL in days** (takes the minimum of usage & mass projections):
  ```
  RUL = min( (U_rating − U_accum)/(ū_daily × δ), (M_rating − M_proc)/(m̄_daily × δ) )
  ```
- `SwitchMeter(...)` → meter-breakdown fallback switch.

## Key invariants

- A `Machine` cannot be its own downstream edge.
- `Measure` requires `ρ > 0` and `κ_moisture ≥ 1.0`.
- `WearPart.Create` requires a non-empty name.
- RUL always returns `≥ 0` (clamped at zero when fully worn).
