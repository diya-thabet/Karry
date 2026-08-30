# Backend — Database & Persistence

> **Layer:** `src/backend/Karry.Infrastructure/Persistence/` · **Related:** [`02-domain.md`](02-domain.md) · [`../architecture.md`](../architecture.md) § Data Model

---

## Current state (Phase 1)

- **Provider:** PostgreSQL 16 + PostGIS via Npgsql + `UseNetTopologySuite()`.
- **DbContext:** `KarryDbContext : DbContext, IUnitOfWork`.
  - Stamps `tenant_id` on newly-added `ITenantScoped` entities.
  - Updates `UpdatedAtUtc` on `Modified` entities (via `SaveChangesAsync` override).
- **Entity configurations:** `IEntityTypeConfiguration<T>` — `Machine` (`machines`), `WearPart` (`wear_parts`), `Tenant` (`tenants`), plus Phase 1 identity/RBAC/audit/unit-pref tables.
- **Design-time:** `KarryDbContextFactory : IDesignTimeDbContextFactory<KarryDbContext>` builds its own `IConfiguration` (JSON + env) so `dotnet ef` works headless in CI.
- **Repository:** `GenericRepository<T> : IRepository<T>`.
- **Migration:** opt-in auto-migrate on startup (`Database:AutoMigrate`, on in Development).
- **Executed migration:** `20260830150249_InitialIdentityRbac` — creates `users`, `roles`, `role_permissions`, `permissions`, `refresh_tokens`, `tenant_unit_preferences`, `user_unit_preferences`, `audit_log`.
- **Seeder:** `DbSeeder` — idempotent permission catalog + platform super-admin (`Seed:Enabled`, `Seed:AdminEmail`, **`Seed:AdminPassword` required**).

## Planned schema (Phase 2 target)

- **Site/Blast:** `sites`, `blast_patterns` (PostGIS `geometry`), `blasts`, `production_runs`
- **Equipment:** `machines`, `machine_nodes` (graph edges JSONB), `wear_parts`, `wear_usage_logs`, `maintenance_events`
- **Shifts/Verification:** `shifts`, `shift_entries`, `entry_audit_overrides`, `scale_tickets`
- **Warehouse:** `warehouses`, `inventory_items`, `stock_movements`, `transfer_waybills`, `asset_transfers`, `physical_inventory_audits`
- **Financial:** `ledger_entries` (append-only), `loans`, `amortization_schedules`, `operational_costs`
- **PKI:** `signatures`, `signed_documents`
- **Weather/Analytics:** `weather_forecasts`, `production_telemetry`

## Tenancy strategy (RLS) — implemented

- Every tenant-scoped table gets `tenant_id` + PostgreSQL Row-Level Security (RLS).
- `TenantContextMiddleware` (in Api) exposes the JWT `tenant_id` claim via `HttpContext.Items`.
- **`RowLevelSecurityInterceptor`** (a `DbConnectionInterceptor` in `Karry.Infrastructure`) issues `SET app.current_tenant_id = '<id>'` per connection, injected via the `(sp, options)` `AddDbContext` overload.
- Hand-authored RLS policies (`USING (tenant_id = current_setting('app.current_tenant_id')::uuid)`) are in the migration `Up()` for `users`, `roles`, `tenant_unit_preferences`, `user_unit_preferences`, `audit_log`, `machines`, `wear_parts` (and reversed in `Down()`):
  ```sql
  CREATE POLICY tenant_isolation_policy ON users
  USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
  ```
- **Integration proof:** `SecurityEdgeCaseTests.TenantIsolation_OtherTenantsUsersNotVisible` asserts one tenant's admin cannot list another tenant's users (runs against real Postgres in CI).

## Migration workflow

```bash
cd src/backend
dotnet ef migrations add <Name> --project Karry.Infrastructure --startup-project Karry.Api
make migrate   # applies migrations
```
