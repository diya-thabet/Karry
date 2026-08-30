"""Integration tests for the FastAPI application."""

import pytest
from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_health() -> None:
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_conveyor_endpoint() -> None:
    response = client.post(
        "/engine/conveyor",
        json={"q_nominal": 1.0, "phi_wear": 0.9, "psi_inclination": 0.85, "omega_weather": 0.8},
    )
    assert response.status_code == 200
    assert response.json()["q_belt"] == pytest.approx(1.0 * 0.9 * 0.85 * 0.8)


def test_conveyor_invalid_factor_returns_422() -> None:
    response = client.post(
        "/engine/conveyor",
        json={"q_nominal": 1.0, "phi_wear": 1.5, "psi_inclination": 1.0, "omega_weather": 1.0},
    )
    assert response.status_code == 422


def test_rul_endpoint() -> None:
    response = client.post(
        "/engine/rul",
        json={
            "rating_usage": 5000,
            "accumulated_usage": 1000,
            "daily_usage": 8,
            "rating_mass": 120000,
            "processed_mass": 40000,
            "daily_mass": 400,
            "bond_abrasion_index": 1.4,
        },
    )
    assert response.status_code == 200
    assert response.json()["rul_days"] == pytest.approx((120000 - 40000) / (400 * 1.4))
