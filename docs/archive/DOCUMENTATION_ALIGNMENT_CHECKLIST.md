# Documentation Alignment Checklist

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).  
**Completed:** February 2026 (STEP 1–5). **Living-docs pass:** March 2026 — DOCS_MAP A–P marked DONE; MULTI_COMPANY_STORYBOOK single-company note added; inventory and governance docs refreshed. **Architecture audit (March 2026):** Code vs docs alignment; [ARCHITECTURE_AUDIT_REPORT.md](ARCHITECTURE_AUDIT_REPORT.md) created; architecture/api_surface_summary.md updated (eventing/operational controllers); operations/background_jobs.md updated (all hosted services). **Codebase intelligence (March 2026):** [CODEBASE_INTELLIGENCE_MAP.md](architecture/CODEBASE_INTELLIGENCE_MAP.md) and five relationship maps created; [CODEBASE_INTELLIGENCE_REPORT.md](CODEBASE_INTELLIGENCE_REPORT.md) documents artifacts and findings. **Refactor safety (March 2026):** [REFACTOR_SAFETY_REPORT.md](REFACTOR_SAFETY_REPORT.md) and architecture refactor-safety docs (high_coupling_modules, hidden_dependencies, module_fragility_map, safe/danger zones, refactor_sequence_plan, worker_dependency_risks) created; portal updated. **Architecture watchdog (March 2026):** [ARCHITECTURE_WATCHDOG_REPORT.md](ARCHITECTURE_WATCHDOG_REPORT.md) and watch docs (service_sprawl_watch, controller_sprawl_watch, dependency_leak_watch, worker_coupling_watch, module_boundary_regression) created; drift stable; portal updated.

---

## Complete

| Item | Doc(s) |
|------|--------|
| **A. Product overview** | [overview/product_overview.md](overview/product_overview.md) |
| **B. Business process flows** | [business/process_flows.md](business/process_flows.md) |
| **C. Department & RBAC** | [business/department_rbac.md](business/department_rbac.md) |
| **D. Order lifecycle summary** | [business/order_lifecycle_summary.md](business/order_lifecycle_summary.md) + [01_system/ORDER_LIFECYCLE.md](01_system/ORDER_LIFECYCLE.md) |
| **E. SI app journey** | [business/si_app_journey.md](business/si_app_journey.md) |
| **F. Docket process** | [business/docket_process.md](business/docket_process.md) |
| **G. Billing & MyInvois** | [business/billing_myinvois_flow.md](business/billing_myinvois_flow.md) |
| **H. Inventory/ledger summary** | [business/inventory_ledger_summary.md](business/inventory_ledger_summary.md) |
| **I. Payroll & rate overview** | [business/payroll_rate_overview.md](business/payroll_rate_overview.md) |
| **J. P&L boundaries** | [business/pnl_boundaries.md](business/pnl_boundaries.md) |
| **K. Integrations** | [integrations/overview.md](integrations/overview.md) |
| **L. Background jobs** | [operations/background_jobs.md](operations/background_jobs.md) |
| **M. Scope not handled** | [operations/scope_not_handled.md](operations/scope_not_handled.md) |
| **N. Developer onboarding** | [dev/onboarding.md](dev/onboarding.md) |
| **O. API surface summary** | [architecture/api_surface_summary.md](architecture/api_surface_summary.md) |
| **P. Data model overview** | [architecture/data_model_overview.md](architecture/data_model_overview.md) |
| **Docs map** | [DOCS_MAP.md](DOCS_MAP.md) |
| **Docs inventory** | [DOCS_INVENTORY.md](DOCS_INVENTORY.md) |
| **Changelog** | [CHANGELOG_DOCS.md](CHANGELOG_DOCS.md) |
| **Discrepancies** | [_discrepancies.md](_discrepancies.md) |
| **_INDEX / 00_QUICK_NAVIGATION** | Updated with links to new folders and docs |
| **DOCS_STATUS** | Updated with source-of-truth alignment bullet |
| **Architecture audit report** | [ARCHITECTURE_AUDIT_REPORT.md](ARCHITECTURE_AUDIT_REPORT.md) – Code vs docs; drift fixes; module boundaries. |
| **Codebase intelligence** | [CODEBASE_INTELLIGENCE_MAP.md](architecture/CODEBASE_INTELLIGENCE_MAP.md), [CODEBASE_INTELLIGENCE_REPORT.md](CODEBASE_INTELLIGENCE_REPORT.md), and [architecture/](architecture/) relationship maps (controller_service, module_dependency, background_worker, integration, entity_domain). |
| **Refactor safety** | [REFACTOR_SAFETY_REPORT.md](REFACTOR_SAFETY_REPORT.md) and [architecture/](architecture/) refactor-safety docs (high_coupling_modules, hidden_dependencies, module_fragility_map, safe_refactor_zones, refactor_danger_zones, refactor_sequence_plan, worker_dependency_risks). |
| **Architecture watchdog** | [ARCHITECTURE_WATCHDOG_REPORT.md](ARCHITECTURE_WATCHDOG_REPORT.md) and [architecture/](architecture/) watch docs (service_sprawl_watch, controller_sprawl_watch, dependency_leak_watch, worker_coupling_watch, module_boundary_regression). |

---

## Open – Must Fix (see _discrepancies.md section 2)

- Root README broken refs (⭐_READ_THIS_FIRST, etc.) and single-company note.
- 03_business/MULTI_COMPANY_STORYBOOK.md – add “Outdated: single-company” and link to overview.
- architecture/00_company-systems-overview.md – align to single-company or move to appendix.
- EmailCleanupService double registration (Scoped + HostedService).
- Kingsman/Menorah implementation extent.
- Quotation-to-order flow (any API/UI?).
- Debug log path in Program.cs (gate or remove for production).
- Syncfusion license: document production use of env only.
- Storybook duplication (07_frontend/storybook vs 03_business).
- Optional: 06_ai implementation notes move to appendix.
