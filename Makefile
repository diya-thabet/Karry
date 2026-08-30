.DEFAULT_GOAL := help

COMPOSE := docker compose -f infra/compose.yaml

.PHONY: help bootstrap up down restart logs ps build test lint typecheck \
        math-install math-test backend-run frontend-run clean migrate db-seed \
        validate-compose

help: ## Show available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-16s\033[0m %s\n", $$1, $$2}'

bootstrap: ## First-time setup: copy env, install frontend deps, build images
	cp -n .env.example .env || true
	cp -n src/frontend/.env.example src/frontend/.env.local || true
	cd src/frontend && npm install
	@echo "Bootstrap complete."

up: ## Start the full dev stack
	$(COMPOSE) up --build -d

down: ## Stop the dev stack
	$(COMPOSE) down

logs: ## Tail logs for all services
	$(COMPOSE) logs -f

ps: ## Show running services
	$(COMPOSE) ps

restart: down up ## Restart the stack

build: ## Build images without starting
	$(COMPOSE) build

validate-compose: ## Validate the compose file
	$(COMPOSE) config --quiet && echo "compose config OK"

# --- Tests & quality gates --------------------------------------------------

test: test-backend test-frontend test-math ## Run all test suites

test-backend: ## Run backend tests (requires .NET SDK)
	cd src/backend && dotnet test Karry.sln

test-frontend: ## Run frontend tests & checks
	cd src/frontend && npm run typecheck && npm run lint && npm test

test-math: ## Run math engine tests
	cd src/math-engine && python3 -m pytest

lint: ## Run lint across all projects
	cd src/frontend && npm run lint
	cd src/math-engine && python3 -m ruff check app tests

typecheck: ## TypeScript typecheck
	cd src/frontend && npm run typecheck

# --- Local (non-Docker) dev --------------------------------------------------

math-install: ## Install math engine dev dependencies
	cd src/math-engine && pip install -e '.[dev]'

math-run: ## Run the math engine locally
	cd src/math-engine && uvicorn app.main:app --reload --port 8000

backend-run: ## Run the API locally
	cd src/backend && dotnet run --project Karry.Api

frontend-run: ## Run the PWA locally
	cd src/frontend && npm run dev

# --- Database ----------------------------------------------------------------

migrate: ## Apply EF Core migrations
	cd src/backend && dotnet ef database update --project Karry.Infrastructure --startup-project Karry.Api

db-seed: ## Seed the demo tenant (requires migration)
	@echo "Seeding is implemented in Phase 1. Database must be migrated first."

# --- Cleanup -----------------------------------------------------------------

clean: ## Remove build artifacts and stop the stack
	$(COMPOSE) down -v
	cd src/frontend && rm -rf dist node_modules .vite
	@echo "Clean complete."
