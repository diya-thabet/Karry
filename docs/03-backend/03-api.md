# Backend — API Contracts

> **Layer:** `src/backend/Karry.Api/` (host) + `Karry.Application/` (use cases) · **Related:** [`01-architecture.md`](01-architecture.md)

---

## Current endpoints (Phase 1)

| Endpoint | Method | Auth | Purpose |
|---|---|---|---|
| `/api/auth/login` | POST | – | Email + password login (may return 2FA challenge) |
| `/api/auth/two-factor/login` | POST | – | Complete 2FA challenge → tokens |
| `/api/auth/refresh` | POST | – | Rotate refresh token (reuse detection) |
| `/api/auth/logout` | POST | JWT | Revoke refresh token (idempotent) |
| `/api/auth/two-factor/enable` · `/verify` · `/disable` | POST | JWT | 2FA enrolment/verification |
| `/api/tenants` | POST | JWT (platform admin) | Create tenant (seeds roles + admin + unit prefs) |
| `/api/users` | GET/POST | JWT | List / create tenant users |
| `/api/roles` | GET/POST | JWT | List / create roles (GET returns operator role id) |
| `/api/units/convert` | POST | JWT | Dynamic m³ ↔ Tonnes conversion |
| `/api/units/preferences` | PUT/POST | JWT | Per-user unit preference |
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

## API conventions

- **Auth:** JWT Bearer enforced via `[Authorize]`. Anonymous endpoints are the login/2FA/refresh trio.
- **Idempotency:** mutating calls accept an `Idempotency-Key` header (login keyed by email, refresh/logout keyed by token).
- **Errors:** `ExceptionHandlingMiddleware` maps typed application exceptions → HTTP status:
  - `NotFound` → 404 · `Authentication` → 401 · `Forbidden` → 403 · `Conflict` → 409 · `AccountLocked` → **423 Locked** · validation → 400 (RFC-7807 `{ title, detail, code }`).
- **DTOs:** Application-layer records; `KarryMathEngineClient` maps snake_case via `JsonPropertyName`.
- **Swagger:** enabled in Development with Bearer security definition.

## Planned endpoints (from the plan)

See [`../01-planning/IMPLEMENTATION_PLAN.md`](../01-planning/IMPLEMENTATION_PLAN.md) per phase; Phase 2+ adds shift, maintenance, warehouse, scale-ticket, and analytics endpoints.
