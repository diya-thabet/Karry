# Math Engine Architecture (Python FastAPI)

> **Layer:** `src/math-engine/` · **Extracted from** the cross-cutting [`../architecture.md`](../architecture.md).
> **Domain formulas:** [`../02-reference/codex.tex`](../02-reference/codex.tex)

---

## Design decision

**Stateless HTTP microservice** behind the .NET API. The API is the single source of truth for persistence; the math engine computes and returns JSON. This isolates numerical work (NumPy) and lets it scale independently.

```
src/math-engine/
├── pyproject.toml          # fastapi, uvicorn, numpy, pydantic; dev: pytest, httpx, ruff
├── conftest.py             # ensures `app` package importable by pytest
├── app/
│   ├── main.py             # FastAPI app: /health, /engine/conveyor, /engine/rul
│   ├── core/
│   │   ├── conveyor.py     # compute_q_belt(...)
│   │   └── rul.py          # compute_rul_days(...)
│   └── schemas/__init__.py # Pydantic request/response models (snake_case)
└── tests/                  # unit + API tests
```

## Contracts (snake_case JSON, mirrors codex)

- `POST /engine/conveyor`
  - Request: `{ q_nominal, phi_wear, psi_inclination, omega_weather }`
  - Response: `{ q_belt }`
  - Model: `Q_belt = q_nominal × phi_wear × psi_inclination × omega_weather`; each factor ∈ (0, 1].
- `POST /engine/rul`
  - Request: `{ rating_usage, accumulated_usage, daily_usage, rating_mass, processed_mass, daily_mass, bond_abrasion_index }`
  - Response: `{ rul_days }`
  - Model: `RUL = min(usageDays, massDays)` (minimum of the two projections).

The .NET `KarryMathEngineClient` uses `JsonPropertyName` to map these snake_case fields.

## Tooling & conventions

- **Ruff** linter (`E,F,W,I,UP,B,SIM`, line-length 120); config in `pyproject.toml`.
- `conftest.py` at project root so pytest can import the `app` package regardless of invocation dir.
- Tests: pure unit tests (`tests/test_conveyor.py`, `test_rul.py`) + HTTP API tests (`test_api.py` via `fastapi.testclient`).
- Run: `make math-run` → uvicorn on :8000; docs at `/docs`.

## Roadmap

Phase 4 adds the closed-loop DCG mass-balance solver and fleet solvency/weather endpoints.
