"""Hybrid predictive maintenance: Remaining Useful Life (RUL) engine.

RUL_m(p) = min( (U_rating - U_accum) / (u_bar_daily * delta_abrasion),
                (M_rating - M_processed) / (m_bar_daily * delta_abrasion) )
"""

from __future__ import annotations


def compute_rul_days(
    rating_usage: float,
    accumulated_usage: float,
    daily_usage: float,
    rating_mass: float,
    processed_mass: float,
    daily_mass: float,
    bond_abrasion_index: float = 1.0,
) -> float:
    """Compute remaining useful life in days, taking the minimum of usage and mass projections."""
    if daily_usage <= 0 and daily_mass <= 0:
        raise ValueError("At least one daily rate must be positive.")
    if bond_abrasion_index <= 0:
        raise ValueError("bond_abrasion_index must be positive.")

    usage_left = max(0.0, rating_usage - accumulated_usage)
    mass_left = max(0.0, rating_mass - processed_mass)

    usage_days = usage_left / (daily_usage * bond_abrasion_index) if daily_usage > 0 else float("inf")
    mass_days = mass_left / (daily_mass * bond_abrasion_index) if daily_mass > 0 else float("inf")

    return min(usage_days, mass_days)
