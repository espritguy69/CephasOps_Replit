# Final Go/No-Go SaaS Readiness Criteria

**Date:** 2026-03-13

Defines what **must** pass before CephasOps can be declared **SaaS-ready** for real multi-tenant usage. These criteria are the gate for sign-off; conditional go and waivers must be documented.

**Final production readiness:** See [SAAS_PRODUCTION_READINESS_REPORT.md](SAAS_PRODUCTION_READINESS_REPORT.md) for the autonomous pass outcome, critical fixes (UsersController, WarehousesController), verdicts by area, and go recommendation.

---

## 1. Mandatory (must pass — no waiver)

| ID | Criterion | How verified | Owner |
|----|-----------|--------------|-------|
| **M1** | **Tenant resolution and guard** — Every authenticated API request (except allowlisted paths) has a resolved tenant (company); requests without valid tenant are blocked with 403. | TenantGuardMiddleware and TenantProvider; integration test: request without company → 403. | Backend |
| **M2** | **List isolation** — All major list endpoints (orders, buildings, service-installers, partners, invoices, materials, departments, parser drafts, notifications) return only the current tenant’s data when called as a tenant user. | Two-tenant integration test per list endpoint. | Backend |
| **M3** | **Detail-by-ID isolation** — GET by ID for any tenant-scoped resource (order, invoice, building, SI, partner, material, file, document) returns 404 (or 403) when the ID belongs to another tenant. | Integration test: as tenant A, GET with tenant B’s ID → 404. | Backend |
| **M4** | **Report and export scope** — Report run and all exports (orders-list, stock-summary, ledger, materials-list, scheduler-utilization) use companyId from current tenant only (no query parameter override); department scope is enforced and restricted to current tenant’s departments. | ReportsController uses _tenantProvider.CurrentTenantId; ResolveDepartmentScopeAsync; integration test and manual export check. | Backend / QA |
| **M5** | **Background job tenant scope** — Every tenant-scoped background job runs under TenantScopeExecutor with that job’s CompanyId (or payload company); no tenant-scoped job writes another tenant’s data. | BackgroundJobProcessorService uses RunWithTenantScopeOrBypassAsync(job.CompanyId ?? payload); integration test; see [09_background_job_tenant_safety.md](09_background_job_tenant_safety.md). | Backend |
| **M6** | **Event dispatch and replay scope** — Event store dispatch and replay use entry.CompanyId for scope; no handler runs in wrong tenant context. | EventStoreDispatcherHostedService and EventReplayService use RunWithTenantScopeOrBypassAsync(entry.CompanyId); tenant-safety regression suite. | Backend |
| **M7** | **Single-tenant full lifecycle** — At least one tenant can complete the full order lifecycle (draft → assign → SI complete → docket → invoice → MyInvois → payment → completed) without error and without cross-tenant data. | Manual UAT per [08_single_tenant_regression_plan.md](08_single_tenant_regression_plan.md) and [02_manual_uat_plan.md](02_manual_uat_plan.md) Stage 4. | QA |
| **M8** | **SI app tenant scope** — SI app shows only the logged-in SI’s assigned jobs for their company; accessing another tenant’s order ID returns 404 or error. | Manual UAT Stage 5; optional E2E. | QA / Frontend-SI |
| **M9** | **No critical or high risk confirmed** — No **Critical** or **High** item in [03_high_risk_areas.md](03_high_risk_areas.md) is confirmed (reproducible) without a fix or accepted mitigation. | Risk register review; test results. | Product / Eng |

---

## 2. Strongly recommended (should pass — waiver requires sign-off)

| ID | Criterion | How verified | Waiver condition |
|----|-----------|--------------|-------------------|
| **S1** | **Search and autocomplete** — Order search, material/building/partner autocomplete return only current tenant’s data. | Integration test; manual check. | Documented workaround (e.g. no search in v1) and fix date. |
| **S2** | **Payroll and P&L** — Payroll run and P&L rebuild and report use only current tenant’s orders and rates. | Service and job scope; UAT. | Same. |
| **S3** | **Notifications** — Notifications are created and dispatched only for the correct tenant; retention runs per-tenant or under controlled bypass. | NotificationService and NotificationRetentionService; tests. | Same. |
| **S4** | **File and document access** — File/document download by ID returns 404 when ID belongs to another tenant; uploads store correct CompanyId. | Integration test; manual. | Same. |
| **S5** | **Department scope** — Department-scoped endpoints (e.g. report run) reject departmentId that belongs to another tenant with 403. | ResolveDepartmentScopeAsync; integration test. | Same. |
| **S6** | **Tenant onboarding** — New company can be provisioned and first user can log in and use the system in isolation. | TenantProvisioningController / provisioning flow; UAT. | Launch with existing tenants only; onboarding fix by date. |

---

## 3. Conditional go

**Conditional go** is acceptable only if:

- All **Mandatory** criteria (M1–M9) pass.
- Every **Strongly recommended** failure has a **written waiver** with: (a) description of gap, (b) workaround or restriction (e.g. “no search in v1”, “onboarding manual only”), (c) owner and target fix date, (d) sign-off from product and tech lead.
- No **Critical** risk from [03_high_risk_areas.md](03_high_risk_areas.md) remains unmitigated.
- Launch scope is explicitly limited (e.g. “invite-only tenants”, “no new tenant self-service”) until waivers are closed.

---

## 4. No-go conditions

Declare **no-go** if any of the following:

- **M1–M9** — Any mandatory criterion fails and cannot be fixed before launch.
- **Data leakage** — Any confirmed case of one tenant seeing or modifying another tenant’s data (list, detail, report, export, file, notification, job side effect).
- **Single-tenant regression** — Core order lifecycle or SI app flow is broken for a single tenant (e.g. cannot complete order, cannot assign, SI app empty or wrong data) and no workaround.
- **Unmitigated Critical risk** — A Critical risk in the register is confirmed and no fix or mitigation is in place.

---

## 5. Sign-off

| Role | Responsibility |
|------|-----------------|
| **QA / UAT lead** | Confirm M7, M8, and manual UAT stages passed; evidence attached. |
| **Backend / platform** | Confirm M1–M6, M9; tenant-safety and integration tests passed; risk register reviewed. |
| **Product** | Accept mandatory and strongly-recommended criteria; approve any waivers for conditional go. |
| **Tech lead** | Sign off on technical readiness and waiver acceptability. |

**Final declaration:**

- **Go** — All mandatory and strongly recommended criteria pass; no open Critical/High risks; CephasOps is SaaS-ready for the agreed launch scope.
- **Conditional go** — All mandatory criteria pass; waivers documented and signed off; launch scope limited as agreed.
- **No-go** — One or more mandatory criteria fail or unmitigated Critical risk; do not launch until resolved.

---

## 6. Traceability

- Mandatory criteria map to [01_master_checklist.md](01_master_checklist.md) sections 1 (tenant isolation), 2 (single-tenant regression), 6 (background jobs), 8 (data visibility).
- Execution evidence from [05_execution_order.md](05_execution_order.md) Phases 1–7 and [02_manual_uat_plan.md](02_manual_uat_plan.md) should be collected and referenced in sign-off.
