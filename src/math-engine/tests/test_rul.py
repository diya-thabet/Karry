"""Unit tests for the RUL engine."""

import pytest

from app.core.rul import compute_rul_days


def test_usage_driven_rul() -> None:
    # usage: (5000 - 1000) / (8 * 1.4) = 357.14
    # mass:  (120000 - 40000) / (400 * 1.4) = 142.86  -> mass binds (minimum)
    result = compute_rul_days(
        rating_usage=5000,
        accumulated_usage=1000,
        daily_usage=8,
        rating_mass=120000,
        processed_mass=40000,
        daily_mass=400,
        bond_abrasion_index=1.4,
    )
    assert result == pytest.approx((120000 - 40000) / (400 * 1.4), rel=1e-6)


def test_minimum_of_usage_and_mass() -> None:
    # usage: (1000-900)/10 = 10 days; mass: (5000-100)/50 = 98 days -> 10
    result = compute_rul_days(1000, 900, 10, 5000, 100, 50, 1.0)
    assert result == pytest.approx(10.0)


def test_fully_worn_returns_zero() -> None:
    result = compute_rul_days(100, 100, 10, 10000, 10000, 20, 1.0)
    assert result == 0.0


def test_requires_positive_daily_rate() -> None:
    with pytest.raises(ValueError):
        compute_rul_days(100, 0, 0, 100, 0, 0, 1.0)
