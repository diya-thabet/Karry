#!/usr/bin/env bash
# First-time developer bootstrap for the Karry Platform.
# Copies env templates, installs frontend dependencies, and verifies the toolchain.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

echo "==> Karry bootstrap starting"

if [[ ! -f .env ]]; then
  cp .env.example .env
  echo "    Created .env from .env.example"
fi

if [[ ! -f src/frontend/.env.local ]]; then
  cp src/frontend/.env.example src/frontend/.env.local
  echo "    Created frontend .env.local from .env.example"
fi

if command -v node >/dev/null 2>&1; then
  echo "==> Installing frontend dependencies"
  (cd src/frontend && npm install)
fi

if command -v dotnet >/dev/null 2>&1; then
  echo "==> Restoring backend solution"
  (cd src/backend && dotnet restore Karry.sln)
fi

if command -v docker >/dev/null 2>&1; then
  echo "==> Building dev images"
  docker compose -f infra/compose.yaml build
else
  echo "    Docker not found — skipping image build (run via Makefile once Docker is installed)."
fi

echo "==> Bootstrap complete."
echo "    Next:  make up   (full stack)   or   make <service>-run (local dev)"
