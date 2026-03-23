# Automatic Tenant Boundary Test Suite — Summary

## What was added

- **Tenant boundary surface inventory** (`TENANT_BOUNDARY_SURFACE_INVENTORY.md`): Table of tenant-sensitive controllers/surfaces, category, risk, protections, and whether new tests are required.
- **Test framework and fixtures**:
  - **TenantBoundaryTestFixture**: Seeds two tenants (Company A/B, User A/B, Department A/B, DepartmentMemberships). Idempotent `SeedAsync()`.
  - **BoundaryTestClientBuilder**: Extension `ForTenant(HttpClient, userId, companyId, roles)` to set `X-Test-User-Id`, `X-Test-Company-Id`, `X-Test-Roles`.
- **Core automatic boundary tests** (`TenantBoundaryTests.cs`):
  - **Users**: List returns only same-tenant users; get by id other tenant → 404; list with search does not leak other tenant.
  - **Warehouses**: List only same-tenant (create via API then assert); get/update/delete other tenant → 404/403; get-all with other companyId → 403.
  - **Rates (rate cards)**: List isolation (A does not see B’s cards); get/update/delete other tenant → 404/403; without company context → 403 or 200 (department fallback).
  - **Files**: List isolation; get metadata / download / delete other tenant → 404; without company context → 401/403 or 200 (department fallback).
  - **Reports**: Definitions 200 or 403; run (orders-list) and export (stock-summary) with tenant + department → 200/403/404; tenant-scoped.
  - **Settings (workflow guard conditions)**: List returns only same-tenant definitions (no cross-tenant IDs).
  - **Departments**: List returns only same-tenant departments.
  - **Payment terms**: With company 200/NotFound; without company 403.
  - **Time slots**: Without company 403.
- **Phase 3 (financial, inventory, operational)**: Billing invoices list + get-by-id 404; Bins list/get/update/delete + list with other companyId 403; Inventory materials list isolation; Audit log list with other companyId 403; EventStore get event 404; Background job runs list + get-by-id 404.
- **Search boundary**: Users list with `?search=User` does not return other-tenant users.
- **Docs**: `TEST_FRAMEWORK.md`, `SEARCH_BOUNDARY_COVERAGE.md`, `WORKFLOW_SIDE_EFFECT_TESTS.md`, `RISKY_PATTERN_ENFORCEMENT.md`, `BOUNDARY_TEST_COVERAGE_MAP.md`, and this summary.

## What is now automatically protected

- Cross-tenant **read** (list and by-id) for Users, Warehouses, Departments, **Rate cards**, **Files**, **Guard condition definitions**, **Billing invoices**, **Bins**, **Inventory materials**, **Audit log**, **EventStore events**, **Background job runs**: blocked (404 or list isolation).
- Cross-tenant **write/delete** for Warehouses, **Rate cards**, and **Bins**: blocked (404/403).
- **Files**: cross-tenant metadata/download/delete → 404.
- **companyId override** on warehouses and **bins**: non–SuperAdmin gets 403.
- **Audit log**: list with other companyId → 403; non–SuperAdmin forced to own company.
- **Missing company context**: Payment terms, time slots, rates, files return 403 (or 200 when department fallback supplies company in test).
- **Reports** run/export: tenant + department scoped (200/403/404 as applicable).
- **Search** on users: no cross-tenant leak.

## Critical gaps found and fixed

- **Warehouse 500 in test host (fixed)**: Warehouse create returned 500 in integration tests because the `Warehouse` entity was not included in the EF Core model (`ApplicationDbContext`). The service used `_context.Set<Warehouse>()`, which throws when the type is not registered. **Remediation**: Added `DbSet<Warehouse> Warehouses` to `ApplicationDbContext` and created `WarehouseConfiguration` in `Configurations/Settings/WarehouseConfiguration.cs` so the entity is mapped to table `Warehouses`. The four previously skipped warehouse tenant-isolation tests (List, GetById, Update, Delete) now run and pass. See `docs/tenant_boundary_tests/WAREHOUSE_TEST_REMEDIATION.md`.
- **Phase 3 — Bins tenant isolation (fixed)**: BinsController did not enforce tenant scope: list accepted any companyId, get/update/delete did not check bin ownership. **Remediation**: RequireCompanyId on all bin endpoints; validate companyId for list/create; get/update/delete return 404 when bin belongs to another tenant. See `BOUNDARY_EXPANSION_PHASE3.md`.
- **Phase 3 — Audit log tenant scope (fixed)**: LogsController passed through companyId to the service without restriction; non–SuperAdmin could request another company’s audit. **Remediation**: Non–SuperAdmin must have company context; companyId is forced to current tenant; requesting another company returns 403.
- **Phase 3 — IBinService not registered (fixed)**: BinsController failed in tests (and would fail in production) with "Unable to resolve IBinService". **Remediation**: Registered `IBinService`/`BinService` in `Program.cs`.

## Remaining medium gaps

- **Orders, Documents, Scheduler, SI App, Notifications**: No API-level boundary tests yet; existing integration/application tests partially cover some. Billing (invoices list/get 404), Inventory (materials list), EventStore (get event 404), Background job runs (list/get 404), Bins, and Audit log now have boundary tests.
- **Rates/Files**: List isolation tests assert cross-tenant exclusion only (no “contain own” in test host when list can be empty); get/update/delete other-tenant are covered.
- **Workflow side effects**: Order→notifications, invoice by-id cross-tenant not yet covered by automated tests.
- **Risky patterns**: No automated grep/analyzer in CI; enforcement is test-based and review-based.

## Expansion (Phase 2) — Rates, Files, Reports, Settings

Additional tenant-boundary tests were added for:

- **Rates (rate cards)**: List isolation, get/update/delete other tenant 404, without company 403 or 200.
- **Files**: List isolation, metadata/download/delete other tenant 404, without company 401/403 or 200.
- **Reports**: Stock-summary export and orders-list run with tenant + department; definitions already covered.
- **Settings**: Guard condition definitions list (workflow guard-conditions) — no cross-tenant IDs.

No real tenant-isolation bugs were found. In the test host, “without company context” (no `X-Test-Company-Id`) can still return 200 when department fallback supplies a company; tests accept 403 or 200 for that case. List tests assert only cross-tenant exclusion (A must not see B’s IDs, B must not see A’s IDs).

## Expansion (Phase 3) — Billing, Bins, Inventory, Audit, EventStore, Job runs

Additional tenant-boundary tests and fixes:

- **Billing (invoices)**: List isolation; get-by-id other tenant → 404.
- **Bins**: List isolation (create via API); list with other companyId → 403; get/update/delete other tenant → 404. **Fixes**: BinsController now uses RequireCompanyId and validates bin ownership; IBinService registered in Program.cs.
- **Inventory (materials)**: List isolation (no cross-tenant IDs).
- **Audit log**: List with other companyId → 403. **Fix**: LogsController now forces companyId to current tenant for non–SuperAdmin and returns 403 when requesting another company.
- **EventStore**: Get event by id (other tenant or unknown id) → 404/403.
- **Background job runs**: List isolation; get-by-id other tenant → 404.

38 tenant-boundary tests pass in total. See `BOUNDARY_EXPANSION_PHASE3.md`.

## Phase 4 — Architectural Tenant Safety Guards

Architectural safeguards were added so that tenant-owned writes cannot occur without tenant context and developers cannot accidentally bypass tenant scope silently.

- **Tenant-owned entities:** All entities inheriting `CompanyScopedEntity`, plus User, BackgroundJob, JobExecution, OrderPayoutSnapshot, InboundWebhookReceipt, **Warehouse**, **Bin**, **AuditLog**, **JobRun** are enforced in SaveChanges.
- **SaveChanges / SaveChangesAsync:** `ApplicationDbContext` validates before persist: (1) no tenant-scoped entity may be saved without tenant context (or platform bypass); (2) every such entity’s CompanyId must match current tenant. Sync `SaveChanges()` now also runs this validation.
- **TenantScopeGuard:** New helper `TenantScopeGuard.RequireTenantContext()` (delegates to `TenantSafetyGuard.AssertTenantContext`) for use in background jobs, batch operations, import pipelines, and maintenance tasks to fail fast when tenant context is missing.
- **IgnoreQueryFilters and raw SQL:** All usages were reviewed and documented in `TENANT_SAFETY_GUARDS.md`; each either has an explicit tenant filter or runs under a documented platform bypass.
- **Documentation:** `docs/tenant_boundary_tests/TENANT_SAFETY_GUARDS.md` describes what guards exist, where enforcement happens, how platform bypass works, and rules developers must follow.

No business behaviour changes; guard logic triggers only for incorrect usage (missing tenant context or CompanyId mismatch). All tenant-boundary tests continue to pass.

## Phase 5 — CI Enforcement for Tenant Safety

Tenant-safety verification is **mandatory in CI**. The workflow `.github/workflows/tenant-safety.yml` runs on every pull request and push to `main`/`master` when backend, analyzers, or tools change. It:

- **Builds** `CephasOps.Application` and `CephasOps.Api` (API build also enforces analyzer CEPHAS001/CEPHAS004).
- **Runs Tenant Safety Invariants**: `CephasOps.Application.Tests` filtered to `TenantSafetyInvariantTests` and `SaveChangesTenantIntegrity` (Phase 4 guard behaviour).
- **Runs Tenant Boundary Tests**: `CephasOps.Api.Tests` filtered to `TenantBoundaryTests` (API-level isolation).

If any of these steps fail, the pipeline fails; PRs cannot merge with a red tenant-safety check. See **TENANT_SAFETY_CI_ENFORCEMENT.md** for where each check lives, exact commands, and why they are mandatory.

## Phase 6 — Production Hardening for Tenant Safety

Production hardening makes tenant-safety issues **visible, diagnosable, and operationally controlled** in live environments.

- **Guard violation observability:** Structured logging (category `PlatformGuardViolation`) with GuardName, Operation, Message, CompanyId, EntityType; API request path and correlation ID when a tenant guard exception is handled. No sensitive payloads.
- **Production-safe responses:** Tenant guard failures return **403 Forbidden** with a safe message ("Invalid request context."); internal details only in logs.
- **Metrics:** OpenTelemetry meter `CephasOps.TenantSafety`: counters for guard violations, missing tenant context attempts, and platform bypass usage (scrape at `/metrics` when enabled).
- **Startup validation:** At startup (non-Testing), the app validates that `ITenantProvider` and `ApplicationDbContext` are registered; missing registration fails fast.
- **Health / diagnostics:** Recent violations are buffered in `IGuardViolationBuffer` (in-memory, bounded); no public tenant-safety endpoint by default—use for internal dashboards or admin tooling if needed.
- **Deployment checklist, alerting, runbook:** See **TENANT_SAFETY_PRODUCTION_HARDENING.md** and **TENANT_SAFETY_RUNBOOK.md**.

## How to run the suite

From repo root:

```bash
cd backend/tests/CephasOps.Api.Tests
dotnet test --filter "FullyQualifiedName~TenantBoundaryTests"
```

To run all integration tests (including other tenant-related tests):

```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

Or run the full Api.Tests project:

```bash
dotnet test
```

## How to extend

1. **New tenant-facing endpoint**: Add a test in `TenantBoundaryTests` (or a new class in the same collection) that:
   - Calls `await _boundaryFixture.SeedAsync();`
   - Creates clients with `_factory.CreateClient().ForTenant(userId, companyId)` for Tenant A and B;
   - Calls the endpoint as A with B’s resource id (or list and assert B’s data not present);
   - Asserts status 403 or 404 (and list content only for same tenant).
2. **New surface**: Add a row to `TENANT_BOUNDARY_SURFACE_INVENTORY.md` and, if high risk, add tests to `BOUNDARY_TEST_COVERAGE_MAP.md`.
3. **Search/lookup**: Add a test that uses the same list endpoint with a search parameter and asserts no cross-tenant results; document in `SEARCH_BOUNDARY_COVERAGE.md`.

## References

- **Surface inventory**: `docs/tenant_boundary_tests/TENANT_BOUNDARY_SURFACE_INVENTORY.md`
- **Test framework**: `docs/tenant_boundary_tests/TEST_FRAMEWORK.md`
- **Coverage map**: `docs/tenant_boundary_tests/BOUNDARY_TEST_COVERAGE_MAP.md`
- **Tenant safety guards (Phase 4)**: `docs/tenant_boundary_tests/TENANT_SAFETY_GUARDS.md`
- **Tenant safety CI enforcement (Phase 5)**: `docs/tenant_boundary_tests/TENANT_SAFETY_CI_ENFORCEMENT.md`
- **Tenant safety production hardening (Phase 6)**: `docs/tenant_boundary_tests/TENANT_SAFETY_PRODUCTION_HARDENING.md`
- **Tenant safety runbook**: `docs/tenant_boundary_tests/TENANT_SAFETY_RUNBOOK.md`
- **Launch checklist**: `docs/launch_readiness/GO_LIVE_CHECKLIST.md` (updated to reference this suite)
