# Tenant Boundary Test Framework

This document describes the test infrastructure used for tenant boundary tests in CephasOps and how to author tests that assert tenant isolation (no cross-tenant read/write/delete/list/file access).

---

## Host and database

- **CephasOpsWebApplicationFactory** (`CephasOps.Api.Tests/Infrastructure/CephasOpsWebApplicationFactory.cs`): `WebApplicationFactory<Program>` that:
  - Sets **environment to `"Testing"`**, which triggers:
    - **In-memory database**: `ApplicationDbContext` uses `UseInMemoryDatabase("CephasOpsIntegrationTests")` (see `Program.cs` when `EnvironmentName == "Testing"`).
    - **Test auth**: default scheme is test auth so no real JWT is required.
  - Registers **TestAuthenticationHandler** as the default authenticate/sign-in/challenge scheme.

Tests use `IClassFixture<CephasOpsWebApplicationFactory>` and share the same in-memory DB instance within the collection (e.g. `[Collection("InventoryIntegration")]`).

---

## Test authentication (headers)

**TestAuthenticationHandler** (`CephasOps.Api.Tests/Infrastructure/TestAuthenticationHandler.cs`) builds a `ClaimsPrincipal` from request headers:

| Header | Purpose |
|--------|--------|
| **X-Test-User-Id** | Required. User ID (GUID); sets `NameIdentifier` and `sub`. |
| **X-Test-Company-Id** | Optional. Tenant/company ID (GUID); sets `companyId` and `company_id` claims. Omit to simulate “no company context” (expect 403 on tenant endpoints). |
| **X-Test-Roles** | Optional. Comma-separated roles (e.g. `Admin`, `Member`, `SuperAdmin`). Default `Member`. |

Use these headers on every `HttpClient` request in tests to simulate a user in a specific tenant (and optionally role). In **Testing**, the app uses `TestUserPermissionProvider` so permissions align with these claims.

---

## Provisioning Tenant A and Tenant B

- **Tenant A / B**: Use two distinct GUIDs, e.g. `companyA = Guid.NewGuid()` and `companyB = Guid.NewGuid()`.
- **Users**: Use distinct user GUIDs per tenant (e.g. `userA`, `userB`) and set `X-Test-User-Id` and `X-Test-Company-Id` accordingly.
- **Seeding**:
  - **Option 1 – DbContext in test**: Create a scope from `_factory.Services`, resolve `ApplicationDbContext`, and insert entities (e.g. `Company`, `Order`, `Department`) with the desired `CompanyId`. In **Testing**, the same in-memory DB is shared; ensure each test that mutates data either uses a fresh DB (if the factory is configured so) or cleans/isolates data to avoid collisions.
  - **Option 2 – API**: Use an `HttpClient` configured with Tenant A (or B) and call create endpoints to seed orders, users, etc.; then use a second client with the other tenant to assert no visibility.

Existing patterns: `TenantIsolationIntegrationTests` and `ReportsIntegrationTests` use a single shared factory and seed via `ApplicationDbContext` in helper methods (e.g. `SeedDepartmentScopeDataAsync`), then create an `HttpClient` with the appropriate `X-Test-Company-Id` and `X-Test-User-Id`.

---

## Creating an HttpClient for Tenant A vs Tenant B

- **Client for Tenant A**:  
  `var clientA = _factory.CreateClient();`  
  Set headers: `X-Test-User-Id` = user A, `X-Test-Company-Id` = company A, `X-Test-Roles` = e.g. `Admin`.
- **Client for Tenant B**:  
  `var clientB = _factory.CreateClient();`  
  Set headers: `X-Test-User-Id` = user B, `X-Test-Company-Id` = company B, `X-Test-Roles` = e.g. `Admin`.

Use `clientA` to create or fetch resources that belong to Tenant A; use `clientB` to assert that B cannot see or modify A’s resources (and vice versa).

**Helpers**: **TenantBoundaryTestFixture** (`Infrastructure/TenantBoundaryTestFixture.cs`) seeds two tenants (Company A/B, User A/B, Department A/B, DepartmentMemberships). Call `await _boundaryFixture.SeedAsync()` in each test (idempotent). **BoundaryTestClientBuilder** (`Infrastructure/BoundaryTestClientBuilder.cs`) provides `ForTenant(this HttpClient client, Guid userId, Guid companyId, string roles = "Admin")` to set `X-Test-User-Id`, `X-Test-Company-Id`, and `X-Test-Roles`.

---

## What to assert (200 / 403 / 404)

- **200 OK**: Valid request within the same tenant (e.g. list orders as Tenant A, get order by id that belongs to A).
- **403 Forbidden**: Request that crosses tenant boundary (e.g. Tenant A user requests with `companyId=B` on a list endpoint, or accesses another tenant’s resource). Also when company context is missing (no `X-Test-Company-Id`) on tenant-scoped endpoints.
- **404 Not Found**: Request by id for a resource that does not exist or belongs to another tenant (prefer 404 over leaking existence).

**List-endpoint isolation**: As Tenant A, call `GET /api/orders` (and similar list endpoints). Response must only contain entities for Tenant A. Then seed an entity for Tenant B and call again as Tenant A; the list must still not include B’s entity.

**Update/delete cross-tenant**: As Tenant A, obtain an id that belongs to Tenant B (e.g. from seed). Call `PUT` or `DELETE` for that id as Tenant A. Expect **403** or **404**, not 200.

**File access cross-tenant**: As Tenant A, upload a file (or use a pre-seeded file id for A). As Tenant B, call download or get-by-id for that file (or A’s id). Expect **403** or **404**, not 200. Same for document generation/preview and report export URLs that reference another tenant’s data.

---

## Reference

- **Existing integration tests**: `TenantIsolationIntegrationTests`, `AdminApiSafetyTests`, `InventoryDepartmentAccessIntegrationTests`, `ReportsIntegrationTests` (see `CephasOps.Api.Tests/Integration`).
- **Surface inventory**: `TENANT_BOUNDARY_SURFACE_INVENTORY.md` in this folder.
