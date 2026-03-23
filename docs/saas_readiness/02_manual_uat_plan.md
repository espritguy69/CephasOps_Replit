# Step-by-Step Manual UAT Plan — SaaS Readiness

**Date:** 2026-03-13

Staged manual UAT plan for QA or product ops to execute. Assumes CephasOps backend and admin frontend (and optionally SI app) are running; tenant safety architecture is in place.

---

## 1. Setup

### 1.1 Environment

- Backend: `ASPNETCORE_ENVIRONMENT=Development`, PostgreSQL with `cephasops` database.
- Admin frontend: dev server (e.g. port 5173); API proxy to backend (e.g. port 5000).
- SI app (optional): `frontend-si` dev server.
- At least two companies (tenants) with seeded or created data:
  - **Tenant A (e.g. Cephas):** CompanyId = &lt;guid-A&gt;, has users, departments, orders, SIs, materials, email accounts.
  - **Tenant B (e.g. second company):** CompanyId = &lt;guid-B&gt;, has users, departments, orders, SIs, materials.

### 1.2 Tenant creation assumptions

- **Option A:** Use existing seeded companies (e.g. Cephas + second company from seed).
- **Option B:** Use TenantProvisioningController / Company provisioning to create Tenant B; then create a user and department for Tenant B.
- **Assumption:** Each test user has a single primary company (JWT companyId or department→company resolution). SuperAdmin can use X-Company-Id header to switch context for support only.
- **Evidence:** Document CompanyIds and at least one user per tenant (email, role) for UAT.

### 1.3 Persona matrix (minimal)

| Persona | Tenant | Role / permissions | Use for |
|--------|--------|--------------------|--------|
| Admin A | Tenant A | Admin (or Scheduler + Orders) | Full flow in Tenant A; list/detail/report/export |
| SI A | Tenant A | Service installer (SI linked to user) | SI app: assigned jobs, transitions, photos |
| Admin B | Tenant B | Admin | Full flow in Tenant B; isolation checks |
| Viewer A | Tenant A | Reports.view only | Report run and export only |
| SuperAdmin | Platform | SuperAdmin | Tenant switch (X-Company-Id), provisioning, admin tools |

---

## 2. Staged workflows and expected outcomes

### Stage 1 — Auth and tenant resolution

| Step | Action | Expected outcome | Pass/fail |
|------|--------|------------------|-----------|
| 1.1 | Login as Admin A (Tenant A). | 200; JWT contains companyId = Tenant A. Dashboard shows Tenant A data only. | |
| 1.2 | Login as Admin B (Tenant B). | 200; JWT contains companyId = Tenant B. Dashboard shows Tenant B data only. | |
| 1.3 | Call an API that requires tenant (e.g. GET /api/orders) without a valid token or with token that has no company. | 401 or 403; TenantGuardMiddleware blocks if company unresolved. | |
| 1.4 | As SuperAdmin, send GET /api/orders with X-Company-Id: &lt;Tenant B id&gt;. | 200; response contains only Tenant B orders. | |
| 1.5 | As Admin A, send GET /api/orders with X-Company-Id: &lt;Tenant B id&gt; (non-SuperAdmin). | X-Company-Id ignored; response contains Tenant A orders only (or 403 if no tenant). | |

**Evidence:** Screenshot of network tab (JWT payload or response), or Postman/curl log.

---

### Stage 2 — List and detail isolation

| Step | Action | Expected outcome | Pass/fail |
|------|--------|------------------|-----------|
| 2.1 | As Admin A: GET /api/orders, GET /api/buildings, GET /api/service-installers. | All lists contain only Tenant A data. | |
| 2.2 | As Admin B: same list endpoints. | All lists contain only Tenant B data. | |
| 2.3 | As Admin A: GET /api/orders/{id} with an order ID that belongs to Tenant B. | 404 (or 403). Never return Tenant B order body. | |
| 2.4 | As Admin A: GET /api/invoices/{id} with Tenant B’s invoice ID. | 404 (or 403). | |
| 2.5 | As Admin A: search orders (if search endpoint exists) with a keyword that matches only Tenant B. | No Tenant B results. | |

**Evidence:** List counts, sample IDs; 404 response for cross-tenant detail.

---

### Stage 3 — Reports and exports

| Step | Action | Expected outcome | Pass/fail |
|------|--------|------------------|-----------|
| 3.1 | As Admin A: run “orders-list” report (API or UI) with department scope. | Rows only from Tenant A. | |
| 3.2 | As Admin A: export orders-list (csv/xlsx). | File contains only Tenant A orders. | |
| 3.3 | As Admin A: run stock-summary and ledger reports; export. | Data and files scoped to Tenant A. | |
| 3.4 | As Admin B: run same report types. | Data and files scoped to Tenant B only. | |
| 3.5 | Verify no report endpoint accepts companyId as query parameter to override tenant. | companyId from _tenantProvider only (per code). | |

**Evidence:** Export file row count or sample rows; confirm companyId column or IDs match current tenant.

---

### Stage 4 — Single-tenant full order lifecycle (Tenant A)

| Step | Action | Expected outcome | Pass/fail |
|------|--------|------------------|-----------|
| 4.1 | Create or use a parsed draft; convert to order (or create order manually). | Order created with Tenant A CompanyId. | |
| 4.2 | Assign order to an SI (Tenant A); set slot. | Assignment and slot saved; SI sees job in SI app. | |
| 4.3 | In SI app (as SI A): start job, MetCustomer, complete with splitter/port/serial if required. | Order moves to OrderCompleted; event store has CompanyId. | |
| 4.4 | Mark dockets received; verify docket; upload to partner (or simulate). | Docket workflow completes. | |
| 4.5 | Generate invoice; submit to MyInvois (or mock). | Invoice and submission tied to Tenant A. | |
| 4.6 | Record payment; mark order Completed. | Order in Completed; P&L and payroll reflect Tenant A only. | |

**Evidence:** Order ID, status progression, one invoice number, P&L snapshot for Tenant A.

---

### Stage 5 — SI app tenant scope

| Step | Action | Expected outcome | Pass/fail |
|------|--------|------------------|-----------|
| 5.1 | Login to SI app as SI A (Tenant A). | Only jobs assigned to that SI in Tenant A appear. | |
| 5.2 | As SI A: open a job, perform transition (e.g. OnTheWay → MetCustomer). | Transition succeeds; order remains Tenant A’s. | |
| 5.3 | Attempt to access (if possible) a job ID that belongs to Tenant B (e.g. direct URL or API). | 404 or 403; no Tenant B data returned. | |
| 5.4 | As SI for Tenant B: login SI app. | Only Tenant B assigned jobs. | |

**Evidence:** Job list screens; transition success; 404 for cross-tenant job access.

---

### Stage 6 — Background jobs and notifications

| Step | Action | Expected outcome | Pass/fail |
|------|--------|------------------|-----------|
| 6.1 | Trigger a tenant-scoped job (e.g. P&L rebuild for Tenant A). | Job runs; P&L data updated for Tenant A only; logs show correct CompanyId. | |
| 6.2 | Trigger email ingestion for Tenant A’s mailbox. | Emails and drafts created under Tenant A only. | |
| 6.3 | Create a notification (e.g. order assigned) in Tenant A. | Notification and dispatch created with Tenant A; recipient is Tenant A user. | |
| 6.4 | Check job/execution observability (e.g. SystemWorkersController, job dashboard). | Job runs show tenant where applicable; no cross-tenant data in logs. | |

**Evidence:** Job run record with CompanyId; notification recipient list; observability screenshot.

---

### Stage 7 — Permissions and department scope

| Step | Action | Expected outcome | Pass/fail |
|------|--------|------------------|-----------|
| 7.1 | As Viewer A (reports.view only): run report, export. | Allowed. | |
| 7.2 | As Viewer A: attempt to edit order or create user. | 403. | |
| 7.3 | As Admin A with Department X: run report with department scope X. | Data for Department X only (within Tenant A). | |
| 7.4 | As Admin A: run report with department scope that belongs to Tenant B. | 403 (department not in user’s tenant). | |

**Evidence:** 403 responses; report data limited to allowed department.

---

### Stage 8 — Files and documents

| Step | Action | Expected outcome | Pass/fail |
|------|--------|------------------|-----------|
| 8.1 | As Admin A: upload file/document for an order (Tenant A). | File stored with Tenant A scope; downloadable. | |
| 8.2 | As Admin A: attempt to access file by ID that belongs to Tenant B (if ID known). | 404 or 403. | |
| 8.3 | Use document template for Tenant A to generate document. | Generated document contains Tenant A data only. | |

**Evidence:** File download success; 404 for cross-tenant file ID.

---

## 3. Pass/fail guidance

- **Pass:** Outcome matches “Expected outcome” and no cross-tenant data is visible or writable.
- **Fail:** Any cross-tenant data leak (list, detail, report, export, file, notification), or tenant-scoped operation failing incorrectly (e.g. 403 when same-tenant), or single-tenant flow broken (order lifecycle, invoice, payroll).
- **Blocking:** Any failure in Stage 1 (auth/tenant resolution), Stage 2 (list/detail isolation), or Stage 3 (reports/exports) is **release-blocking** for SaaS. Stage 4–8 failures are prioritized per [03_high_risk_areas.md](03_high_risk_areas.md).

---

## 4. Evidence to capture

- For each stage: pass/fail per step; screenshot or export sample where useful.
- For isolation tests: request (URL, headers, user) and response (status, body snippet or “empty list” / “404”).
- For full lifecycle: order ID, status progression, invoice number, and P&L or payroll impact (summary).
- Store in a shared folder or test run report; reference in go/no-go decision per [10_go_no_go_criteria.md](10_go_no_go_criteria.md).
