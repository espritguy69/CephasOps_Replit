# Tenant Boundary Surface Inventory

This document inventories tenant-sensitive API surfaces in `backend/src/CephasOps.Api/Controllers` for the Automatic Tenant Boundary Test Suite. Each row represents a controller or endpoint area that can read, write, list, search, report, or serve files in a tenant context.

**Reference tests for "existing coverage":**
- **Api.Tests:** `TenantIsolationIntegrationTests`, `AdminApiSafetyTests`, `InventoryDepartmentAccessIntegrationTests`, `ReportsIntegrationTests`
- **Application.Tests:** `BillingServiceFinancialIsolationTests`, `OrderPayoutSnapshotServiceFinancialIsolationTests`

---

## Surface inventory table

| Controller / Surface | Category | Tenant risk | Current protections | Automated coverage exists | New tests required |
|----------------------|----------|-------------|----------------------|---------------------------|--------------------|
| **OrdersController** (`api/orders`) | read, write, delete, list, search, report | High | ITenantProvider, department scope, global filter on Order | Partial (Application: payout snapshot) | Yes – list isolation, get-by-id cross-tenant 404, update/delete cross-tenant 403/404 |
| **UsersController** (`api/users`) | read, write, list, search | High | ITenantProvider, RequireCompanyId | No | Yes – list isolation, by-id cross-tenant |
| **RatesController** (`api/rates`) | read, write, list, search | High | ITenantProvider, IFieldLevelSecurityFilter, PermissionCatalog | No | Yes – list/resolve scoped to tenant, cross-tenant 403 |
| **WarehousesController** (`api/warehouses`) | read, write, list, by-id | High | ITenantProvider, companyId vs currentTenant check, IsSuperAdmin, department scope | Yes (TenantIsolation: companyId override 403) | Yes – list isolation, by-id cross-tenant 404 |
| **FilesController** (`api/files`) | read, write, delete, list, file | High | ITenantProvider (CurrentTenantId), Unauthorized when no company | No | Yes – upload/download/list scoped; cross-tenant file access 403/404 |
| **DocumentsController** (`api/documents`) | read, write, report, file | High | ITenantProvider (CurrentTenantId), Unauthorized when no company | No | Yes – generate/preview scoped; cross-tenant 403 |
| **ReportsController** (`api/reports`) | list, report, search, file (export) | High | ITenantProvider, IDepartmentAccessService, department scope on run/export | Yes (ReportsIntegrationTests: run/export dept A vs B 403, definitions 200) | Expand – cross-tenant report run/export 403 |
| **BillingController** (`api/billing`) | read, write, list, report | High | ITenantProvider | Yes (Application: BillingServiceFinancialIsolationTests) | Yes – API-level list/by-id/update cross-tenant |
| **BillingRatecardController** (`api/billing/ratecards`) | read, write, list | High | Tenant-scoped via billing services | No | Yes – list and by-id isolation |
| **InventoryController** (`api/inventory`) | read, write, list, search, report, file | High | ITenantProvider, RequireCompanyId, department scope (materials) | Yes (InventoryDepartmentAccessIntegrationTests: materials dept A/B) | Yes – list isolation, ledger/export cross-tenant 403 |
| **SchedulerController** (`api/scheduler`) | read, write, list, search | High | ITenantProvider | No | Yes – list/slots/jobs scoped; cross-tenant 403 |
| **SiAppController** (`api/si-app`) | read, write, list, search | High | ITenantProvider (CurrentTenantId) | No | Yes – SI-facing list/jobs/orders scoped; cross-tenant 403 |
| **NotificationsController** (`api/notifications`) | read, write, list | Medium | ITenantProvider (CurrentTenantId) | No | Yes – list/status scoped to tenant |
| **EventStoreController** (`api/event-store`) | read, list, search | High | ITenantProvider, ScopeCompanyId(), companyId query vs scope 403 | Yes (TenantIsolation + AdminApiSafety: companyId override 403, replay 404) | Optional – more event/replay cross-tenant cases |
| **EventLedgerController** (`api/event-store/ledger`) | read, list | High | Same as EventStore pattern | No | Yes – list scoped to tenant |
| **OnboardingController** (`api/onboarding`) | read, write, list | Medium | ITenantProvider (CurrentTenantId) | No | Yes – onboarding state scoped; cross-tenant 403 |
| **InfrastructureController** (`api/buildings/{id}/infrastructure`) | read, write, list | High | ITenantProvider, RequireCompanyId (building implies tenant) | No | Yes – building-scoped; cross-tenant building 404 |
| **PartnersController** (`api/partners`) | read, write, list, by-id | High | ITenantProvider (CurrentTenantId) | No | Yes – list and by-id isolation |
| **BuildingsController** (`api/buildings`) | read, write, list, search | High | ITenantProvider (CurrentTenantId) | No | Yes – list and by-id isolation |
| **AssetsController** (`api/assets`) | read, write, list, by-id | High | ITenantProvider, RequireCompanyId | No | Yes – list and by-id isolation |
| **PaymentsController** (`api/payments`) | read, write, list | High | ITenantProvider, RequireCompanyId | No | Yes – list and by-id isolation |
| **InvoiceSubmissionsController** (`api/invoices`) | read, write, list | High | Service-level (invoice ownership); no ITenantProvider in controller | No | Yes – verify service uses tenant; by-invoiceId cross-tenant 404 |
| **SupplierInvoicesController** (`api/supplier-invoices`) | read, write, list | High | ITenantProvider, RequireCompanyId | No | Yes – list and by-id isolation |
| **AdminUsersController** (`api/admin/users`) | read, write, list | Medium (platform admin) | Platform/admin policy; cross-tenant visibility intended | Yes (AdminApiSafetyTests style) | Optional – ensure admin cannot be used to leak tenant data without policy |
| **AdminController** (`api/admin`) | read, list | Medium (platform) | Admin policy | No | Optional – platform-only |
| **DepartmentsController** (`api/departments`) | read, write, list | High | ITenantProvider; service resolves company from user context | No | Yes – list only returns user’s company departments; cross-tenant 403 |
| **TasksController** (`api/tasks`) | read, write, list | High | ITenantProvider, RequireCompanyId | No | Yes – list and by-id isolation |
| **WorkflowController** / **WorkflowDefinitionsController** (`api/workflow`, `api/workflow-definitions`) | read, write, list | High | ITenantProvider, RequireCompanyId | No | Yes – list and run scoped; cross-tenant 403 |
| **ReportDefinitionsController** (`api/report-definitions`) | read, list | Medium | Tenant-scoped definitions | No | Yes – list isolation |
| **DocumentTemplatesController** (`api/document-templates`) | read, write, list | High | ITenantProvider (CurrentTenantId) | No | Yes – list and by-id isolation |
| **PaymentTermsController** (`api/payment-terms`) | read, write, list | High | ITenantProvider, RequireCompanyId | Yes (TenantIsolation: with/without company 200/403) | Optional – by-id cross-tenant 404 |
| **TimeSlotsController** (`api/time-slots`) | read, write, list | Medium | ITenantProvider / RequireCompanyId | Yes (TenantIsolation: with/without company 200/403) | Optional – list isolation |
| **ControlPlaneController** (`api/admin/control-plane`) | write, list | Low (platform) | Admin/platform only | No | Optional |
| **OperationsOverviewController** (`api/admin/operations`) | read, list | Low (platform) | ITenantProvider, admin | No | Optional |

---

## Category legend

- **read** – get by id or single resource
- **write** – create, update
- **delete** – delete resource
- **list** – list/paginated list
- **search** – search or filter
- **report** – run report or analytics
- **file** – upload, download, or export file

## Tenant risk levels

- **High** – Direct tenant data (orders, users, billing, inventory, files, documents, reports, assets, payments). Cross-tenant access must return 403 or 404.
- **Medium** – Tenant-scoped but lower impact (notifications, onboarding) or platform admin with tenant visibility.
- **Low** – Platform-only or control-plane; no tenant data exposure.

## Next steps

1. Add boundary tests for all **High**-risk surfaces marked "New tests required": two tenants (A/B), seed data per tenant, HttpClient per tenant, assert list isolation and cross-tenant 403/404.
2. For **Reports** and **Inventory**, extend existing integration tests to explicit tenant A vs B (in addition to department A vs B).
3. For **Application.Tests** (Billing, OrderPayoutSnapshot), keep as-is; add API-level tests using `CephasOpsWebApplicationFactory` and test auth headers.
