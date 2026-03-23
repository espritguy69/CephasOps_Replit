# CephasOps SaaS Readiness Testing Package

**Version:** 1.0  
**Date:** 2026-03-13  
**Purpose:** Execution-ready testing package to validate CephasOps readiness for real SaaS usage across multiple tenants while preserving single-company behavior inside each tenant.

---

## 1. Package contents

| Document | Purpose |
|---------|--------|
| [01_master_checklist.md](01_master_checklist.md) | Practical structured checklist covering all major application surfaces |
| [02_manual_uat_plan.md](02_manual_uat_plan.md) | Staged UAT plan: setup, tenant assumptions, persona-based flows, pass/fail, evidence |
| [03_high_risk_areas.md](03_high_risk_areas.md) | High-risk conversion risk register (single-company → SaaS) by severity/likelihood |
| [04_automated_test_scenarios.md](04_automated_test_scenarios.md) | Automated test cases to add or strengthen (integration, API, workflow, jobs, UI, search/export, cross-tenant, concurrency) |
| [05_execution_order.md](05_execution_order.md) | Recommended order to run tests from foundational validation through readiness signoff |
| [06_module_test_matrix.md](06_module_test_matrix.md) | Module-to-SaaS coverage: Auth, Companies, Orders, Parser, Scheduler, SI app, Inventory, Billing, Payroll, P&L, Reports, Notifications, Workflow, Background jobs, Files, Admin |
| [07_tenant_isolation_attack_surface.md](07_tenant_isolation_attack_surface.md) | Tenant isolation attack-surface checklist (list, detail-by-ID, search, reports, exports, jobs, notifications, files, deep links, cache, admin) |
| [08_single_tenant_regression_plan.md](08_single_tenant_regression_plan.md) | How to verify each tenant still behaves like original single-company CephasOps |
| [09_background_job_tenant_safety.md](09_background_job_tenant_safety.md) | Background job and automation tenant-safety plan (ownership, context, retry, platform vs tenant, failure containment, observability) |
| [10_go_no_go_criteria.md](10_go_no_go_criteria.md) | Final go/no-go SaaS readiness criteria |
| [SAAS_READINESS_EXECUTIVE_SUMMARY.md](SAAS_READINESS_EXECUTIVE_SUMMARY.md) | Executive summary, critical risks, assumptions, document placement rationale |

---

## 2. How to use this package

- **QA / UAT lead:** Start with [02_manual_uat_plan.md](02_manual_uat_plan.md) and [05_execution_order.md](05_execution_order.md). Use [01_master_checklist.md](01_master_checklist.md) to track progress.
- **Engineering:** Use [04_automated_test_scenarios.md](04_automated_test_scenarios.md) and [06_module_test_matrix.md](06_module_test_matrix.md) to add or strengthen tests; [07_tenant_isolation_attack_surface.md](07_tenant_isolation_attack_surface.md) for security-focused checks.
- **Release / product:** Use [10_go_no_go_criteria.md](10_go_no_go_criteria.md) for signoff and [03_high_risk_areas.md](03_high_risk_areas.md) for risk awareness.
- **Platform / ops:** Use [09_background_job_tenant_safety.md](09_background_job_tenant_safety.md) for job observability and tenant-safety validation.

---

## 3. Repository context

- **Backend:** ASP.NET Core 10, EF Core, PostgreSQL; tenant boundary = **CompanyId**; execution boundary = **TenantScopeExecutor** (see `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md`).
- **Frontend:** React 18 + Vite + TypeScript (admin); SI app in `frontend-si/`.
- **Business flow:** Inbound partner work order ingestion → draft/order creation → assignment/scheduling → SI field execution → blockers/reschedules → docket handling → invoice generation and MyInvois → payment tracking → SI payroll and P&L/reporting.

---

## 4. Alignment with existing docs

This package aligns with and references:

- `backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md`
- `backend/docs/architecture/TENANT_SCOPE_EXECUTOR_COMPLETION.md`
- `backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md`
- `docs/architecture/SAAS_MULTI_TENANT_AUDIT.md`
- `docs/02_modules/background_jobs/OVERVIEW.md`
- `docs/03_business/BUSINESS_POLICIES.md`
- `docs/07_frontend/SI_APP_OVERVIEW.md`
- `docs/dev/onboarding.md`

Where existing tests (e.g. tenant-safety regression suite in the developer guide) already cover areas, this package reuses and aligns rather than duplicating.
