# LLM Hands-Off Notes (Lessons Learned)

> **Purpose:** A living log of problems encountered while building Karry, with the exact root cause and fix — so a future LLM (or human) does **not** repeat the same mistakes.
>
> **Convention:** Append a new entry at the **bottom** each time you hit a non-obvious issue. Keep it precise: symptom → root cause → fix. If a fix is an architectural decision, also reflect it in [`../architecture.md`](../architecture.md).
>
> **Phase log index:** see [`../07-execution/phase0-foundations-tooling.md`](../07-execution/phase0-foundations-tooling.md) and future `phaseN-*.md` files.

---

## Environment facts (crucial context)

- Authoring machine lacks **Docker** and (initially) the **.NET SDK**. Python 3.14 + Node 22 present.
- To compile/test the backend, the .NET 9 SDK was installed to `/tmp/dotnet` (non-system) and used via:
  ```bash
  export PATH="/tmp/dotnet:$PATH" DOTNET_ROOT=/tmp/dotnet DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
  ```
- When a dev env is missing, still scaffold + verify what you can; document what could not be executed (e.g. Docker).

---

## Issue Log

### 001 — Tailwind `slate` palette colors "do not exist"

**Symptom:** `vite build` failed in PostCSS: *"The `bg-slate-50` class does not exist."*

**Root cause:** In `tailwind.config.js` I *added* a custom color under `theme.extend.colors` named the same as a default palette key:
```js
extend: { colors: { primary: '#142d55', accent: '#2980b9', slate: '#2c3e50' } }
```
`theme.extend` **merges** — but assigning a **string** to `slate` replaced the entire default `slate` scale (`slate-50 … slate-900`) with that single value, so all `slate-NN` utilities vanished.

**Fix:** Never name an extended color the same as a default Tailwind scale unless you intend to replace the whole scale. Removed the custom `slate` override and reused the default `slate` scale:
```js
extend: { colors: { primary: '#142d55', accent: '#2980b9' } }
```

**Rule:** Before overriding a color key, check whether code depends on the default scale shades.

---

### 002 — Short-ton ↔ metric-ton conversion constant was inverted

**Symptom:** Unit test `ShortTons_ToMetricTons_ConvertsCorrectly` failed (got ~1.215 instead of ~1.0).

**Root cause:** In `Karry.Domain/Units/Measure.cs`, the short→metric conversion multiplied by `1.102311310924388` (the metric→short factor) instead of converting correctly. `1 short ton = 0.90718474 metric tons`.

**Fix:** Define one clear constant and use it consistently in all three path points (`ToMass`, `ToVolume`, `ToMetricTons`):
```csharp
private const decimal MetricTonPerShortTon = 0.90718474m; // 1 st = 0.90718474 t
// short -> metric: multiply by MetricTonPerShortTon
// metric -> short: divide by MetricTonPerShortTon
```

**Rule:** For unit conversions, derive **from a single named constant**, and add a round-trip test. Sanity-check the magnitude (1 st ≈ 0.907 t, so multiplying by 1.10 was clearly wrong).

---

### 003 — RUL "min" semantics vs test author's assumption

**Symptom:** In both the C# `WearPart.ComputeRemaining` test and the Python `compute_rul_days` test, an expectation assumed the *hours/usage* branch binds, but the *mass* branch actually produced the smaller (binding) value.

**Root cause:** The codex RUL formula is `min(usageDays, massDays)` — it takes the **minimum** of the two projections. The test expectation computed only one branch and the test data made the other branch be the true min (e.g. mass: `(120000−40000)/(400×1.4)=142.9` vs usage `(5000−1000)/(8×1.4)=357.1`).

**Fix:** Corrected both test expectations to the actual minimum; where I wanted to isolate the *hours* branch, I set the test data so both branches were equal, and added a separate test where mass binds.

**Rule:** When implementing a `min()`/`max()` formula, write at least one test where **each** branch binds, and compute the expected value by hand first.

---

### 004 — `IServiceCollection` has no `AddValidatorsFromAssembly`

**Symptom:** Backend build error `CS1061: 'IServiceCollection' does not contain a definition for 'AddValidatorsFromAssembly'`.

**Root cause:** The `FluentValidation` package alone does **not** include the DI registration extension. That lives in a separate package.

**Fix:** Added to `Karry.Application.csproj`:
```xml
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
```

**Rule:** FV needs `FluentValidation.DependencyInjectionExtensions` for `AddValidatorsFromAssembly`.

---

### 005 — decimal/double comparison compile error in theory test

**Symptom:** `CS0019: Operator '<' cannot be applied to operands of type 'double' and 'decimal'`.

**Root cause:** A `[Theory]`/`[InlineData(double)]` parameter was compared against a `decimal` literal (`kappa < 1.0m`) where `kappa` was `double`.

**Fix:** Compared against the matching type (`kappa < 1.0`) and cast where needed.

**Rule:** Keep numeric comparisons type-consistent; be careful mixing `double` `[InlineData]` values with `decimal` domain code.

---

### 006 — `IHttpContextAccessor` / `Microsoft.AspNetCore.Http` missing in Infrastructure

**Symptom:** Backend build errors `CS0234/CS0246`: the `Microsoft.AspNetCore.Http` namespace and `IHttpContextAccessor` could not be found in `Karry.Infrastructure`.

**Root cause:** The class library `Karry.Infrastructure` does not automatically reference the ASP.NET Core shared framework (only `Sdk="Microsoft.NET.Sdk"` web projects do).

**Fix:** Added a framework reference to the `.csproj`:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

**Rule:** Any non-web class library that touches `HttpContext`/`IHttpContextAccessor` needs `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.

---

### 007 — `protected` member in a `sealed` class is a build error (warnings-as-errors)

**Symptom:** `CS0628: 'GenericRepository<TEntity>.DbContext': new protected member declared in sealed type` failed the build.

**Root cause:** `Directory.Build.props` sets `TreatWarningsAsErrors=true`. The repository's `DbContext` field was `protected` but the class is `sealed`.

**Fix:** Changed the field to `private readonly`.

**Rule:** With `TreatWarningsAsErrors=true`, any analyzer/style/compiler *warning* becomes a hard error. Expect issues like CS0628, CS1591 (missing XML docs) — the latter is suppressed via `NoWarn=1591`.

---

### 008 — Design-time EF factory must build its own configuration

**Symptom:** (Preventive/structural.) Without proper design-time support, `dotnet ef` cannot create the `DbContext`.

**Root cause:** The design-time host does not run the normal DI pipeline.

**Fix:** `KarryDbContextFactory : IDesignTimeDbContextFactory<KarryDbContext>` builds `IConfiguration` itself (JSON + env vars) and constructs the options with `UseNetTopologySuite()`, mirroring runtime config. The connection string comes from `ConnectionStrings:KarryDatabase`.

**Rule:** Any EF service includes a design-time factory so migrations work headless in CI.

---

### 009 — Top-level `Program.cs` doesn't see extension-method namespaces automatically

**Symptom:** `CS1061: 'WebApplication' does not contain a definition for 'MigrateDatabaseAsync'`.

**Root cause:** `MigrateDatabaseAsync` is an extension method in namespace `Karry.Api`; a top-level `Program.cs` is in the global namespace and does **not** auto-import sibling project namespaces.

**Fix:** Added explicit usings at the top of `Program.cs`:
```csharp
using Karry.Api;
using Karry.Api.Middleware;
```

**Rule:** In top-level statement files, always add explicit `using` for extension methods and types in other namespaces of the same project.

---

### 010 — `KarryDbContext` didn't implement `IUnitOfWork`

**Symptom:** `CS0266/CS1662`: cannot convert `KarryDbContext` to `IUnitOfWork` in DI registration.

**Root cause:** I registered `IUnitOfWork → KarryDbContext` (`AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<KarryDbContext>())`) but the `DbContext` did not implement that interface.

**Fix:** Made `KarryDbContext : DbContext, IUnitOfWork` (its `SaveChangesAsync(CancellationToken)` already satisfies the contract).

**Rule:** When registering a concrete type as an interface, the type must actually implement the interface.

---

### 011 — Duplicate test method names after a rename

**Symptom:** `CS0111: Type 'WearPartTests' already defines a member called 'ComputeRemaining_TakesMinimumOfUsageAndMass'`.

**Root cause:** A partial edit renamed a test to a name that already existed in the same class.

**Fix:** Gave the test a distinct name (`..._WhenMassBinds`); later rewritten as two clearly-named tests (hours-binding, mass-binding).

**Rule:** After editing/renaming test methods, re-check the file for name collisions; use a tool to list method names if unsure.

---

### 012 — Tailwind `@apply` on `body` required the color scale to exist (subset of 001)

**Symptom:** `index.css` used `@apply bg-slate-50 text-slate-900` — failed until issue 001 was fixed.

**Root cause:** Same Tailwind palette override issue.

**Fix:** Resolved by 001.

**Rule:** If you use `@apply`, ensure the referenced utilities actually resolve at build time.

---

### 013 — Dotnet not installed; plan approach for verification

**Symptom:** Environment lacked an OS-level `dotnet`.

**Root cause:** Sandbox provisioning.

**Fix:** Installed SDK via the official installer script to a non-system dir for verification only:
```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 9.0 --install-dir /tmp/dotnet
export PATH="/tmp/dotnet:$PATH" DOTNET_ROOT=/tmp/dotnet
```
This is a **verification-only** install — do not assume it persists in a fresh environment.

**Rule:** Always attempt to actually compile/test code even if the toolchain isn't preinstalled; document what couldn't be executed.

---

### 014 — SQL client using directive leftover (no compile impact, cleanliness)

**Symptom:** No compile error, but `using Microsoft.Data.SqlClient;` was referenced in the middleware and later removed when middleware was simplified.

**Root cause:** Over-engineered initial middleware; the package was removed before referencing.

**Fix:** Simplified `TenantContextMiddleware` to just expose the claim via `HttpContext.Items`. The **actual** `current_setting('app.current_tenant_id')` injection happens at the DB-connection level in Infrastructure (to be finalized in Phase 1).

**Rule:** Keep the API middleware thin; don't reference DB client namespaces in the web layer.

---

### 015 — Integration tests that need a real DB must NOT live in the main solution

**Symptom:** Adding a `WebApplicationFactory` integration-test project to `Karry.sln` would make `dotnet test Karry.sln` try to connect to Postgres — which doesn't exist on the authoring machine (no Docker/sudo), breaking the local "headless" green run.

**Root cause:** The environment can compile but not run against a live Postgres, and the distribution of a solution folder implies all its test projects run together.

**Fix:** Kept integration tests in a **separate solution** (`Karry.IntegrationTests.sln`) that is **not** referenced by `Karry.sln`. The `WebApplicationFactory` fixture checks for a `ConnectionStrings__KarryDatabase` env var and **skips** when absent, so the project also compiles fine locally. A dedicated CI job starts a `postgres:16` service container, sets the env vars, and runs only that solution.

**Rule:** DB-dependent integration tests → own solution + CI-only execution; keep the fast unit-test solution DB-independent.

---

### 016 — `Seed:AdminPassword` is required and must be injected in the env

**Symptom:** `DbSeeder` constructor throws `InvalidOperationException: Seed:AdminPassword is required.` The default `appsettings.json` only carries `Seed:Enabled` and `Seed:AdminEmail`, so running with `Seed:Enabled=true` and no password crashes.

**Root cause:** The seeder deliberately refuses to guess a super-admin password in a non-development seed (defaults admin email to `root@kar.app` but has no sane default password).

**Fix:** Every seed path (CI integration job, compose, prod) sets `Seed__AdminPassword` explicitly. The integration test reads it (falling back to `Karry#RootAdmin1` to match CI).

**Rule:** When enabling the seeder anywhere, supply `Seed:AdminPassword`; do not rely on `appsettings.json` defaults.

---

### 017 — `appsettings.Development.json` forces `Database:AutoMigrate=true`

**Symptom:** Running the backend pointed at a dummy/absent connection string still attempts to migrate because the dev override flips `AutoMigrate` on.

**Root cause:** Phase 0 set `Database:AutoMigrate=true` in `appsettings.Development.json` for the Docker dev flow.

**Fix:** When running the backend locally without a DB, pass `Database__AutoMigrate=false` (env overrides JSON).

**Rule:** Check **both** `appsettings.json` and `appsettings.<Env>.json` before assuming a config flag's effective value; `Development` overrides win locally.

---

### 018 — EF `AddDbContext` overload that injects a `DbConnectionInterceptor`

**Symptom:** Wiring the RLS `DbConnectionInterceptor` into `AddDbContext` — the interceptor must be registered per-connection and needs the current tenant, which is not available in the static/options path.

**Root cause:** `AddDbContext<DbContext>(options => ...)` has no place to pull scoped per-request state for the connection interceptor.

**Fix:** Used the scoped `(IServiceProvider sp, DbContextOptionsBuilder options)` overload of `AddDbContext` so the interceptor can be constructed with access to the per-request session provider.

**Rule:** For connection-level concerns that need request scoping, prefer the `(sp, options)` `AddDbContext` overload over static options.

---

## Future-issue prevention checklist (run before committing backend code)

- [ ] `.csproj` for any library touching HttpContext has `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
- [ ] FluentValidation registration lives in `FluentValidation.DependencyInjectionExtensions` package.
- [ ] No custom Tailwind color name collides with a default palette key whose shades you use.
- [ ] Unit-conversion constants are single-sourced and round-trip tested (short↔metric ton).
- [ ] `min()`/`max()` formula has tests where each branch binds.
- [ ] Top-level `Program.cs` includes explicit usings for sibling extension methods.
- [ ] Sealed classes don't declare `protected` members (warnings-as-errors).
- [ ] No duplicate method names after renames/edits.
- [ ] Design-time EF factory exists for headless migrations.
- [ ] DB-dependent integration tests live in their own solution, gated to CI (skip when no connection string).
- [ ] `Seed:AdminPassword` is set wherever the seeder is enabled (CI, compose, prod).
- [ ] Respect `appsettings.<Env>.json` overrides (e.g. `Development` forces `AutoMigrate=true`).
- [ ] RLS/connection interceptors that need request state use the `(sp, options)` `AddDbContext` overload.

---

## How to contribute

1. Reproduce / hit a new problem → add an `### NNN — <summary>` entry above.
2. Keep the three golden rules: **Symptom → Root cause → Fix**.
3. If the fix changes architecture, update [`../architecture.md`](../architecture.md) too.
4. Reference the phase log that introduced/encountered the issue.
