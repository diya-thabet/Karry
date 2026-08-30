"""Unit tests for the conveyor speed engine."""

import pytest

from app.core.conveyor import compute_q_belt


def test_nominal_with_all_factors_at_one() -> None:
    assert compute_q_belt(1.0, 1.0, 1.0, 1.0) == pytest.approx(1.0)


def test_degraded_factors_reduce_throughput() -> None:
    result = compute_q_belt(1.0, 0.9, 0.85, 0.8)
    assert result == pytest.approx(1.0 * 0.9 * 0.85 * 0.8)


def test_example_delivery_rate() -> None:
    # codex example: 1.0 m^3 per 1.3 minutes baseline
    q_nominal = 1.0 / 1.3
    result = compute_q_belt(q_nominal, 0.85, 0.95, 0.9)
    assert result == pytest.approx(q_nominal * 0.85 * 0.95 * 0.9)


def test_nonpositive_nominal_rejected() -> None:
    with pytest.raises(ValueError):
        compute_q_belt(0.0, 1.0, 1.0, 1.0)


def test_factor_out_of_range_rejected() -> None:
    with pytest.raises(ValueError):
        compute_q_belt(1.0, 1.2, 1.0, 1.0)

    with pytest.raises(ValueError):
        compute_q_belt(1.0, 0.0, 1.0, 1.0)
