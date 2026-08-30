"""Conveyor belt and plant physics engine.

Models the instantaneous volumetric delivery rate Q_belt(t):

    Q_belt(t) = Q_nominal * phi_wear * psi_inclination * omega_weather

where the three dimensionless factors reduce nominal capacity over time.
"""

from __future__ import annotations


def compute_q_belt(
    q_nominal: float,
    phi_wear: float,
    psi_inclination: float,
    omega_weather: float,
) -> float:
    """Compute instantaneous volumetric delivery rate in m^3/min."""
    if q_nominal <= 0:
        raise ValueError("q_nominal must be positive.")
    for factor, name in (
        (phi_wear, "phi_wear"),
        (psi_inclination, "psi_inclination"),
        (omega_weather, "omega_weather"),
    ):
        if not 0.0 < factor <= 1.0:
            raise ValueError(f"{name} must lie in (0, 1].")
    return q_nominal * phi_wear * psi_inclination * omega_weather
