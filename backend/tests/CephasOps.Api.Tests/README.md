# CephasOps.Api.Tests

Integration tests for the CephasOps API using `WebApplicationFactory` with environment `Testing` (in-memory DB and test auth). In Testing, ProductionRoles are disabled (via in-memory config in CephasOpsWebApplicationFactory and optionally appsettings.Testing.json) so no schedulers, job workers, event dispatcher, or notification workers start. Relational-only claim methods in EventStoreRepository, JobExecutionStore, and NotificationDispatchStore no-op when the provider is InMemory, avoiding transaction/GetDbConnection errors.

## Running tests

```bash
cd backend/tests/CephasOps.Api.Tests
dotnet test
```

## Test auth

Tests use `TestAuthenticationHandler`: set user/company/roles via headers.

- `X-Test-User-Id` – required (GUID)
- `X-Test-Company-Id` – optional (GUID)
- `X-Test-Roles` – optional, comma-separated (default: `Member`)

`ICurrentUserService` and department scope resolution use the same user/company; department access is resolved from `DepartmentMemberships` in the in-memory DB.

## Department-scoped tests (InventoryIntegration collection)

Tests that assert department-scoped access (e.g. user in Dept A can access Dept A, cannot access Dept B) seed company, departments, and `DepartmentMemberships` then call the API with the same user/company/department.

In **Testing** environment, `DepartmentAccessService` uses `IgnoreQueryFilters()` when loading `DepartmentMemberships`, so the global tenant/company query filter cannot hide seeded memberships and cause 403s. This fixed the previous 28 department-scoped 403 failures.

**Current status:** 86 passed, 2 skipped. In **Testing**, `StockLedgerService` and `DepartmentAccessService` use `IgnoreQueryFilters()` where needed so ledger/report tests see seeded data. Two tests are skipped: `Return_IncreasesOnHand_WhenAllocationExists_AllocationStatusBecomesReturned` (400 in shared InMemory DB), `ExportOrdersList_FormatPdf_Returns200_AndPdfContentType_AndNonEmptyBytes` (500 in test host). Run `dotnet test` for the latest count.

## Application tests

For unit and application-layer tests (no HTTP), use:

```bash
cd backend/tests/CephasOps.Application.Tests
dotnet test
```

Current status: **770 passed, 7 skipped** (BuildingMatchingService ILike/PostgreSQL, EventBusPhase6 FOR UPDATE SKIP LOCKED, MaterialTemplateService InMemory concurrency).
