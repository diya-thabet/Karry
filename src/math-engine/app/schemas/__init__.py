"""Pydantic request/response schemas for the math engine API."""

from __future__ import annotations

from pydantic import BaseModel, Field


class ConveyorRequest(BaseModel):
    q_nominal: float = Field(gt=0, description="Rated factory baseline speed (m^3/min).")
    phi_wear: float = Field(default=1.0, gt=0, le=1, description="Wear degradation multiplier in (0, 1].")
    psi_inclination: float = Field(default=1.0, gt=0, le=1, description="Inclination factor in (0, 1].")
    omega_weather: float = Field(default=1.0, gt=0, le=1, description="Weather reduction factor in (0, 1].")


class ConveyorResponse(BaseModel):
    q_belt: float


class RulRequest(BaseModel):
    rating_usage: float = Field(gt=0)
    accumulated_usage: float = Field(ge=0)
    daily_usage: float = Field(ge=0)
    rating_mass: float = Field(gt=0)
    processed_mass: float = Field(ge=0)
    daily_mass: float = Field(ge=0)
    bond_abrasion_index: float = Field(default=1.0, gt=0)


class RulResponse(BaseModel):
    rul_days: float
