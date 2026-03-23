# CephasOps API Testing Plan

A simple, practical, and scalable API testing strategy that complements the existing Playwright frontend smoke suite. **No tests or application code are implemented in this document**—planning and structure only.

---

## 1. OVERVIEW

### What is API testing?

API testing exercises the backend HTTP layer: you send requests to endpoints (e.g. `POST /api/auth/login`, `GET /api/orders`) and assert on status codes, response bodies, and headers. It validates contracts, authorization, tenant isolation, and business rules without driving a browser.

### How it differs from Playwright (frontend E2E)

| Aspect | Playwright (frontend) | API tests (backend) |
|--------|------------------------|----------------------|
| **Scope** | Full stack: browser → frontend → API → DB | Backend only: HTTP client → API → DB (or in-memory) |
| **Speed** | Slower (browser, JS, network) | Faster (no UI, in-process or local server) |
| **Flakiness** | Higher (loading, timing, DOM) | Lower (deterministic requests) |
| **Coverage** | User flows, critical pages | Endpoints, auth, tenant isolation, workflows |
| **Run context** | Needs frontend + backend + DB (or mocked) | Usually in-memory DB + test server (e.g. WebApplicationFactory) |

### Why it matters for CephasOps

- **Multi-tenant safety:** API tests can assert that tenant A cannot see or change tenant B’s data (orders, inventory, scheduler).
- **Workflow and domain logic:** Orders, inventory deductions, and scheduler assignments are implemented in the backend; API tests catch regressions without running the full UI.
- **CI efficiency:** Backend API tests run in seconds with an in-memory DB; they complement (not replace) Playwright smoke for a faster feedback loop.
- **Contract stability:** Ensures response shapes and status codes stay consistent for the frontend.

---

## 2. TESTING APPROACH

### Recommendation: integration tests with in-memory test server

- **Use integration tests** that call the real API over HTTP (via `HttpClient` created from `WebApplicationFactory<Program>`). This exercises routing, middleware, auth, and application services together.
- **In-memory test server:** `WebApplicationFactory` (already used in `CephasOps.Api.Tests`) hosts the API in-process. No separate process or real port.
- **Real database vs test database:** Prefer **in-memory database** (e.g. EF Core InMemory or SQLite in-memory) for speed and isolation. The repo already uses **Testing** environment with in-memory DB in `CephasOpsWebApplicationFactory`. For a smaller set of tests that need PostgreSQL-specific behavior, a dedicated test DB is an optional Phase 3 addition.
- **When to mock vs not:**
  - **Do not mock** the HTTP pipeline, auth pipeline, or application services for the main API test suite; use the real stack with test auth and seeded data.
  - **Mock** only external outbound dependencies that are unstable or side-effectful (e.g. email, SMS, third-party HTTP) if they would make tests flaky or slow. Prefer test doubles or in-memory implementations where the app supports it.

### Existing foundation

- **`CephasOpsWebApplicationFactory`** – sets environment to `Testing`, configures `TestAuthenticationHandler` so user/company/roles are set via headers (`X-Test-User-Id`, `X-Test-Company-Id`, `X-Test-Roles`).
- **In-memory DB** – In Testing, the app uses an in-memory database so tests do not require a real PostgreSQL instance.
- **Collections** – e.g. `[Collection("InventoryIntegration")]` for tests that need shared seeding (company, departments, memberships).

New API tests should follow this pattern: same factory, same test auth, and explicit seeding where needed.

---

## 3. KEY API DOMAINS

Endpoints are grouped by domain. For each, we state what to test, important edge cases, and priority (P1 = must-have for first wave, P2 = high value, P3 = later or nightly).

### Auth

| Endpoint / area | What to test | Edge cases | Priority |
|-----------------|--------------|------------|----------|
| `POST /api/auth/login` | 200 + tokens for valid credentials; 401 for invalid; response shape (access token, refresh token, user info) | 403 requires password change; 423 account locked; 403 tenant access denied | P1 |
| Token validation / refresh | Protected endpoint returns 200 with valid token; 401 without or with invalid token | Expired token; wrong tenant in token | P2 |
| `GET /api/auth/me` (or current user) | 200 with user/company/roles when authenticated | 401 when no token | P2 |

### Orders

| Endpoint / area | What to test | Edge cases | Priority |
|-----------------|--------------|------------|----------|
| `GET /api/orders` | 200 and list (or empty) when tenant + department scope valid | 403 when no company; 403 when user has no department access for requested departmentId | P1 |
| `GET /api/orders/{id}` | 200 with order when it belongs to tenant; 404 when not found or wrong tenant | 403 for other tenant’s order | P1 |
| Update order status | 200 for valid transition; 400 for invalid status/transition | Workflow guard failures; concurrency | P2 |
| Create / update order | 201/200 for valid payload; 400 for validation errors | Tenant-scoped creation; department scope | P2 |

### Scheduler

| Endpoint / area | What to test | Edge cases | Priority |
|-----------------|--------------|------------|----------|
| `GET /api/scheduler/calendar` | 200 and calendar DTOs for date range with valid tenant/department | 401 no company; 403 department access denied | P1 |
| Timeline / appointments | 200 and data when scope is valid | Empty range; department filter | P2 |
| Assign installer | 200 when assignment allowed; 400/409 when not (e.g. conflict) | Double-assign; wrong department | P2 |

### Inventory

| Endpoint / area | What to test | Edge cases | Priority |
|-----------------|--------------|------------|----------|
| `GET /api/inventory/materials` | 200 and list when tenant + department scope valid | 403 no company; 403 user not in requested department | P1 |
| `GET /api/inventory/stock-summary` | 200 and summary for permitted department(s) | Empty; multi-location | P2 |
| Ledger: issue / receive / transfer / deduction | 200 for valid operation; 400 when insufficient stock or invalid refs | Idempotency; negative stock; wrong department | P2 |

### Users / Roles

| Endpoint / area | What to test | Edge cases | Priority |
|-----------------|--------------|------------|----------|
| `GET /api/users` (if present) | 200 for admins; 403 for non-admin | Role-based visibility | P2 |
| Permission enforcement | Endpoints return 403 when user lacks required permission | SuperAdmin vs tenant Admin vs Member | P1 (via one or two key endpoints) |
| Department-scoped access | User in Dept A can access A, cannot access B (403) | Already partially covered in ApiSmokeTests | P1 |

### Settings

| Endpoint / area | What to test | Edge cases | Priority |
|-----------------|--------------|------------|----------|
| Company config / departments | 200 for read when tenant context present; 403 without | Tenant isolation | P2 |
| `GET /api/departments` | 200 and list for current company | Empty company; SuperAdmin | P2 |

---

## 4. FIRST 10 API TESTS (HIGH VALUE)

Concrete tests that deliver the most value and align with existing features:

1. **Auth: login success** – `POST /api/auth/login` with seeded (or test) user credentials; expect 200 and response containing access token (and optionally refresh token and user info).
2. **Auth: login invalid credentials** – `POST /api/auth/login` with wrong email/password; expect 401 (and no token).
3. **Orders: get orders returns 200** – `GET /api/orders` with valid test user + company (and optional department); expect 200 and JSON list (possibly empty).
4. **Orders: get orders without company returns 403** – `GET /api/orders` with user but no company context; expect 403 (tenant guard).
5. **Orders: update order status valid flow** – For a seeded order in a valid workflow state, `PATCH` or `PUT` to update status; expect 200 (and optionally verify state in DB or response).
6. **Orders: update order invalid status** – Request invalid or disallowed status transition; expect 400 (or 409 if applicable).
7. **Inventory: deduction or issue works** – Call ledger endpoint (e.g. issue) with valid tenant/department/material; expect 200 and correct balance or ledger entry (if applicable).
8. **Unauthorized access blocked** – Call a protected endpoint (e.g. `GET /api/orders`) without auth (no test headers / no JWT); expect 401.
9. **Tenant isolation: orders** – With two tenants (e.g. TenantBoundaryTestFixture or seeded companies), user A gets 200 for own orders and gets 404 or empty when requesting tenant B’s order by ID (or equivalent).
10. **Health: returns 200 and DB connected** – Already covered in `ApiSmokeTests`; keep as the single “smoke” sanity check for API + in-memory DB.

These stay **read-only or single-step writes** where possible, use existing test auth and seeding patterns, and avoid brittle assumptions about seed data (prefer “200 and valid shape” over exact counts unless needed).

---

## 5. FOLDER STRUCTURE

Suggested layout under the **existing** `backend/tests` structure, without moving or renaming current projects:

```
backend/tests/
  CephasOps.Api.Tests/
    Integration/
      Auth/
        AuthApiTests.cs           # login success, login invalid, optional refresh/me
      Orders/
        OrdersApiTests.cs         # get list, get by id, update status, tenant isolation
      Inventory/
        (existing: InventoryLedgerIntegrationTests.cs, etc.)
        InventoryApiTests.cs      # materials, stock-summary, optional ledger smoke
      Scheduler/
        SchedulerApiTests.cs      # calendar, optional timeline/assign
      (existing files: ApiSmokeTests.cs, TenantIsolationIntegrationTests.cs, ...)
    Infrastructure/
      CephasOpsWebApplicationFactory.cs
      TestAuthenticationHandler.cs
      TenantBoundaryTestFixture.cs
      (existing helpers, e.g. ForTenant extension)
    Common/                        # optional shared helpers
      ApiTestHelpers.cs           # CreateClient with default headers, JSON helpers
      SeedDataBuilder.cs          # optional: company, department, user, order seeds
  CephasOps.Application.Tests/
    (unchanged)
```

- **Integration** – All HTTP API tests; group by domain (Auth, Orders, Inventory, Scheduler). Existing integration tests stay where they are; new ones go into the appropriate domain folder.
- **Infrastructure** – Factory, test auth, fixtures. No change to existing files for this plan.
- **Common** – Optional; add only when duplication justifies it (e.g. shared client creation or seed helpers).

If the repo prefers a flatter layout, a single `Integration/` folder with files like `AuthApiTests.cs`, `OrdersApiTests.cs` is also fine; the important part is consistent naming and use of `CephasOpsWebApplicationFactory` and test auth.

---

## 6. DATA STRATEGY

- **Seeding:** Use the same patterns as today: in-memory DB with `ApplicationDbContext` in a scope created from `CephasOpsWebApplicationFactory.Services`. Seed company, departments, users (if needed for real login), department memberships, and optionally orders/inventory in test setup or via a shared fixture. Prefer **per-test or per-class seeding** where possible so tests do not depend on global state.
- **Isolation:** Each test (or test class) should not rely on order of execution. Use fresh GUIDs for entities, or reset/seed in constructor or method setup so that parallel execution does not cause cross-test leakage.
- **Multi-tenant data:** Reuse or extend patterns like `TenantBoundaryTestFixture`: two companies, two users, each with their own data. Assert that client A cannot see or modify client B’s resources (403/404). Avoid sharing mutable data between tenants across tests.
- **Avoiding conflicts:** Do not share a single “global” order or inventory row across tests that mutate it; either create new entities per test or use read-only checks (e.g. “get list returns 200 and array”) so that concurrent runs do not conflict.

---

## 7. ROADMAP (PHASED)

### Phase 1 – Auth and basic orders (first sprint)

- Add **Auth** integration tests: login success, login invalid credentials (and optionally refresh/me if endpoints exist).
- Add **Orders** integration tests: get orders returns 200 with tenant context; get orders without company returns 403; get order by id returns 200 for own tenant and 404 or 403 for other tenant (reuse or extend tenant boundary fixture).
- Ensure **health** and **correlation/ProblemDetails** remain covered (already in ApiSmokeTests).
- **Deliverable:** New `AuthApiTests.cs` and `OrdersApiTests.cs` (or equivalent), all green in CI with in-memory DB.

### Phase 2 – Inventory and scheduler

- **Inventory:** GET materials and stock-summary with tenant/department; one ledger operation (e.g. issue or receive) that returns 200 and valid shape.
- **Scheduler:** GET calendar (and optional timeline) with tenant/department; assert 200 and structure; optionally one “assign installer” test if stable.
- **Deliverable:** `InventoryApiTests.cs` and `SchedulerApiTests.cs` (or extend existing inventory/scheduler integration tests), no new flakiness.

### Phase 3 – Workflow-heavy and optional PostgreSQL

- Order status transitions and workflow guards; inventory deduction edge cases (insufficient stock, idempotency).
- Optional: a small set of tests against a real PostgreSQL test database for behaviors that differ from in-memory (e.g. concurrency, specific SQL).
- **Deliverable:** Deterministic workflow and edge-case tests; optional test DB job in CI.

---

## 8. GUIDELINES

- **Deterministic:** Do not depend on system time, random IDs in assertions, or execution order. Seed data with fixed or per-test IDs where needed.
- **Avoid flakiness:** No sleep or arbitrary delays; use assertions on response status and content. For async behavior, assert on final state or response rather than polling with random timeouts.
- **Avoid over-mocking:** Prefer the real application pipeline (middleware, auth, services) with test auth and in-memory DB. Mock only external, unreliable dependencies when necessary.
- **Prefer real integration:** Use `WebApplicationFactory` and real HTTP calls; avoid testing controllers in isolation with mocks unless there is a clear reason (e.g. testing a single edge case in a large controller).
- **Tenant and permissions:** Always set tenant (company) and roles via test auth headers where the app expects them; assert 401/403 for missing or wrong context so that tenant isolation and RBAC stay covered.
- **Naming:** Use clear, scenario-based names (e.g. `Login_ValidCredentials_Returns200_AndAccessToken`, `GetOrders_WithoutCompany_Returns403`).

---

## Summary

- **What:** API integration tests against the real ASP.NET Core pipeline using `WebApplicationFactory`, in-memory DB, and header-based test auth.
- **Where:** New tests in `backend/tests/CephasOps.Api.Tests/Integration/` by domain (Auth, Orders, Inventory, Scheduler); reuse existing factory and fixtures.
- **First 10 tests:** Login success/failure, get orders (200/403), order status update (valid/invalid), one inventory operation, unauthorized blocked, tenant isolation for orders, health check.
- **Phases:** 1 – Auth + basic orders; 2 – Inventory + scheduler; 3 – Workflow-heavy and optional PostgreSQL tests.
- **Data:** Seed per test or per class; isolate tenants; no shared mutable state across tests.

---

## Top 5 tests to implement immediately

1. **Login success** – `POST /api/auth/login` with valid credentials; expect 200 and access token in response.
2. **Login invalid credentials** – `POST /api/auth/login` with wrong email/password; expect 401.
3. **Get orders returns 200** – `GET /api/orders` with `X-Test-User-Id`, `X-Test-Company-Id`, and appropriate role; expect 200 and JSON array.
4. **Get orders without company returns 403** – `GET /api/orders` with user but no `X-Test-Company-Id`; expect 403.
5. **Unauthorized access blocked** – `GET /api/orders` (or another protected endpoint) with no auth headers; expect 401.

These five give immediate value: auth contract, tenant guard, and access control, and they fit the existing `CephasOpsWebApplicationFactory` and test auth setup without new infrastructure.
