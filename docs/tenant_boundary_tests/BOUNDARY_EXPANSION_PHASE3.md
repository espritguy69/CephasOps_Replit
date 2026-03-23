# Tenant Boundary Expansion — Phase 3

Phase 3 adds tenant-boundary integration tests for financial, inventory, and operational data surfaces and fixes real tenant-isolation issues found in Bins and Audit log.

## Entities / endpoints covered

| Area | Endpoints | Verification |
|------|-----------|--------------|
| **Billing (invoices)** | GET api/billing/invoices, GET api/billing/invoices/{id} | List isolation; get-by-id other tenant → 404 |
| **Bins** | GET api/bins?companyId=, GET api/bins/{id}, POST api/bins, PUT api/bins/{id}, DELETE api/bins/{id} | List isolation; list with other companyId → 403; get/update/delete other tenant → 404 |
| **Inventory (materials)** | GET api/inventory/materials | List isolation (no cross-tenant IDs) |
| **Audit log** | GET api/logs/audit?companyId= | List with other companyId → 403 |
| **EventStore** | GET api/event-store/events/{eventId} | Get event (other tenant or unknown id) → 404/403 |
| **Background job runs** | GET api/background-jobs/job-runs, GET api/background-jobs/job-runs/{id} | List isolation; get-by-id other tenant → 404 |

## Tests added

- **Invoices_List_ReturnsOnlySameTenantInvoices** — List as A and B; no ID overlap.
- **Invoices_GetById_OtherTenant_Returns404** — Get with random guid → 404.
- **Bins_List_ReturnsOnlySameTenantBins** — Create warehouse + bin per tenant; list only contains own.
- **Bins_List_WithOtherCompanyId_Returns403** — Tenant A requests companyId=B → 403.
- **Bins_GetById_OtherTenant_Returns404** — Tenant A gets Tenant B’s bin → 404.
- **Bins_Update_OtherTenant_Returns404** — Tenant A updates Tenant B’s bin → 404/403.
- **Bins_Delete_OtherTenant_Returns404** — Tenant A deletes Tenant B’s bin → 404/403.
- **Materials_List_ReturnsOnlySameTenantMaterials** — List as A and B; no ID overlap.
- **AuditLog_List_WithOtherCompanyId_Returns403** — Tenant A requests audit?companyId=B → 403.
- **EventStore_GetEvent_OtherTenant_Returns404** — Get event with random guid → 404/403.
- **JobRuns_List_ReturnsOnlySameTenantJobRuns** — List as A and B; no ID overlap.
- **JobRuns_GetById_OtherTenant_Returns404** — Get job run with random guid → 404/403.

Helpers: **CreateBinViaApiAsync**, **GetJobRunIdsFromResponse**.

## Test results

- **38** tenant-boundary tests pass in total (26 existing + 12 new Phase 3).
- All Phase 3 tests pass after fixes.

## Real bugs found and fixed

1. **BinsController tenant isolation**
   - **Issue**: List accepted any `companyId`; get/update/delete did not check bin ownership. A tenant could list another tenant’s bins or get/update/delete another tenant’s bin by id.
   - **Fix**: All bin endpoints use `RequireCompanyId`; list/create validate `companyId` matches tenant (or use scope); get/update/delete return 404 when bin’s `CompanyId` does not match current tenant.

2. **LogsController (audit) tenant scope**
   - **Issue**: `GetAuditLogs` passed through `companyId` from query without restriction; non–SuperAdmin could request another company’s audit log.
   - **Fix**: Non–SuperAdmin must have company context; `companyId` is forced to current tenant; requesting `companyId` different from current tenant returns 403.

3. **IBinService not registered**
   - **Issue**: `IBinService` was never registered in `Program.cs`; BinsController failed with "Unable to resolve service for type 'IBinService'" (tests and production).
   - **Fix**: Added `builder.Services.AddScoped<IBinService, BinService>();` in `Program.cs`.

## Areas confirmed platform-wide

- **Payout health dashboard** (api/payout-health): Dashboard and anomaly endpoints can be platform-wide for SuperAdmin; not covered by tenant-boundary tests in Phase 3.
- **Security activity / security alerts** (api/logs/security-activity, security-alerts): Already restricted to SuperAdmin/Admin roles.

## Areas deferred

- **Payout summaries / snapshots / rate payouts**: No API-level boundary tests in Phase 3; can be added later if endpoints are tenant-scoped.
- **Stock movements / warehouse inventory summaries**: Covered indirectly via materials list; dedicated tests optional.
- **Invoice create/update/delete cross-tenant**: Only get-by-id and list covered; update/delete with other tenant id can be added.
- **EventStore list** (events list with companyId filter): EventStoreController already enforces scope; list isolation test deferred.

## Files changed

- `backend/src/CephasOps.Api/Controllers/BinsController.cs` — RequireCompanyId and bin ownership checks on all endpoints.
- `backend/src/CephasOps.Api/Controllers/LogsController.cs` — Tenant scope for audit log (companyId forced for non–SuperAdmin, 403 for other company).
- `backend/src/CephasOps.Api/Program.cs` — Register `IBinService`/`BinService`.
- `backend/tests/CephasOps.Api.Tests/Integration/TenantBoundaryTests.cs` — 12 new tests + CreateBinViaApiAsync, GetJobRunIdsFromResponse.
- `docs/tenant_boundary_tests/AUTOMATIC_TENANT_BOUNDARY_TEST_SUMMARY.md` — Phase 3 section, protected surfaces, gaps fixed.
- `docs/tenant_boundary_tests/BOUNDARY_TEST_COVERAGE_MAP.md` — Rows for Billing, Bins, Inventory materials, Audit log, EventStore, Background job runs.
- `docs/tenant_boundary_tests/BOUNDARY_EXPANSION_PHASE3.md` — This file.
