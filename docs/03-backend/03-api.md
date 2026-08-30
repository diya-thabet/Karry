# Backend — API Contracts

> **Layer:** `src/backend/Karry.Api/` (host) + `Karry.Application/Units/` (use case) · **Related:** [`01-architecture.md`](01-architecture.md)

---

## Current endpoints (Phase 0)

| Endpoint | Method | Auth | Purpose |
|---|---|---|---|
| `/api/units/convert` | POST | JWT | Dynamic m³ ↔ Tonnes conversion (M = V × ρ × κ_moisture) |
| `/swagger` | GET | – | Swagger UI |

## `POST /api/units/convert`

Request body:
```json
{
  "value": 100,
  "fromUnit": "m3",            // "m3" | "t" | "st"
  "rhoDryTonPerM3": 2.65,      // must be > 0
  "kappaMoisture": 1.1         // must be >= 1.0
}
```

Response:
```json
{
  "value": 291.5,
  "toUnit": "t",
  "appliedDensity": 2.65,
  "appliedMoistureFactor": 1.1
}
```

Behavior:
- `fromUnit = "m3"` → converts to mass (metric or short ton depending on output).
- `fromUnit = "t"` or `"st"` → converts to volume.
- Validated by `ConvertMeasureRequestValidator` (FluentValidation): value/density positive, `κ ≥ 1.0`, unit supported.

## API conventions (growing)

- **Auth:** JWT Bearer enforced via `[Authorize]` (Phase 0 policy wired but most endpoints not yet built).
- **DTOs:** Application-layer records; `KarryMathEngineClient` maps snake_case via `JsonPropertyName`.
- **Swagger:** enabled in Development with Bearer security definition.
- **Error handling:** FluentValidation returns 400s; controller returns `ActionResult` with JSON.

## Planned endpoints (from the plan)

See [`../01-planning/IMPLEMENTATION_PLAN.md`](../01-planning/IMPLEMENTATION_PLAN.md) per phase; Phase 1 adds identity/RBAC, then units-conversion is joined by shift, maintenance, warehouse, scale-ticket, and analytics endpoints in Phases 2–4.
