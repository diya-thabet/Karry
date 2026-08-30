"""Karry math engine API entry point."""

from __future__ import annotations

from fastapi import FastAPI, HTTPException

from app import __version__
from app.core.conveyor import compute_q_belt
from app.core.rul import compute_rul_days
from app.schemas import ConveyorRequest, ConveyorResponse, RulRequest, RulResponse

app = FastAPI(
    title="Karry Math Engine",
    version=__version__,
    description="Dynamic conveyor physics, closed-loop mass balance, RUL and solvency computations.",
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/engine/conveyor", response_model=ConveyorResponse)
def conveyor(request: ConveyorRequest) -> ConveyorResponse:
    try:
        return ConveyorResponse(
            q_belt=compute_q_belt(
                request.q_nominal,
                request.phi_wear,
                request.psi_inclination,
                request.omega_weather,
            )
        )
    except ValueError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc


@app.post("/engine/rul", response_model=RulResponse)
def rul(request: RulRequest) -> RulResponse:
    try:
        return RulResponse(
            rul_days=compute_rul_days(
                request.rating_usage,
                request.accumulated_usage,
                request.daily_usage,
                request.rating_mass,
                request.processed_mass,
                request.daily_mass,
                request.bond_abrasion_index,
            )
        )
    except ValueError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc
