# Boundary Test Coverage Map

| Module | Boundary risk | Automated tests added | Existing tests reused | Remaining gaps | Release-blocking if uncovered |
|--------|----------------|------------------------|------------------------|----------------|---------------------------------|
| **Users** | High | List isolation, by-id 404, search same-tenant | — | By-department (same-tenant) | Yes |
| **Warehouses** | High | List isolation, by-id 404, update/delete 404, companyId override 403 | TenantIsolation (companyId 403) | — | Yes |
| **Rates (rate cards)** | High | List isolation, by-id 404, update/delete 404, without company 403/200 | — | Resolve cross-tenant (optional) | Yes |
| **Files** | High | List isolation, metadata/download/delete 404, without company 401/403/200 | — | — | Yes |
| **Guard condition definitions** | High | List isolation (no cross-tenant IDs) | — | By-id cross-tenant optional | Yes |
| **Departments** | High | List isolation (dept A/B) | — | By-id cross-tenant | Yes |
| **Orders** | High | — | Application payout snapshot | List, by-id, update/delete cross-tenant | Yes |
| **Documents** | High | — | — | Generate/preview cross-tenant | Yes |
| **Reports** | High | Definitions 200/403, run + export with tenant+dept 200/403/404 | ReportsIntegrationTests (run/export dept) | Cross-tenant report run/export | Yes |
| **Billing (invoices)** | High | List isolation, get-by-id 404 | BillingServiceFinancialIsolationTests | Update/delete cross-tenant optional | Yes |
| **Bins** | High | List isolation, list other companyId 403, get/update/delete 404 | — | — | Yes |
| **Inventory (materials)** | High | List isolation (no cross-tenant IDs) | InventoryDepartmentAccessIntegrationTests | Ledger/export cross-tenant | Yes |
| **Scheduler** | High | — | — | List/slots/jobs scoped | Medium |
| **SI App** | High | — | — | Task list/detail, uploads tenant-scoped | Yes |
| **Notifications** | Medium | — | — | List/status tenant-scoped | Medium |
| **Audit log** | High | List with other companyId 403 | — | — | Yes |
| **EventStore** | High | Get event by id 404/403 | TenantIsolation + AdminApiSafety | Event list/replay cross-tenant | Yes |
| **Background job runs** | High | List isolation, get-by-id 404 | — | — | Yes |
| **Payment terms** | High | With/without company 200/403 | TenantIsolation | By-id cross-tenant optional | Yes |
| **Time slots** | Medium | Without company 403 | TenantIsolation | List isolation optional | Medium |
| **Partners, Buildings, Assets, Payments, etc.** | High | — | — | List and by-id isolation | Yes (per surface) |

## Legend

- **Automated tests added**: New tests in `TenantBoundaryTests` or dedicated boundary test classes.
- **Existing tests reused**: TenantIsolationIntegrationTests, AdminApiSafetyTests, ReportsIntegrationTests, InventoryDepartmentAccessIntegrationTests, BillingServiceFinancialIsolationTests.
- **Remaining gaps**: Surfaces where no automatic boundary test exists yet; adding them is recommended.
- **Release-blocking**: Critical tenant data; uncovered cross-tenant access would be a release blocker.

## How to extend

- For each new tenant-facing endpoint: add a test that uses `TenantBoundaryTestFixture` (two tenants), call as Tenant A and with Tenant B’s resource id (or vice versa), assert 403 or 404.
- Reuse `BoundaryTestClientBuilder.ForTenant(client, userId, companyId)` for consistent auth headers.
