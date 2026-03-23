# Architecture Watchdog Summary

**Date:** March 2026  
**Purpose:** Current architecture health, drift detection, major coupling hotspots, and long-term scalability risks. Consolidates Level 5 observability and evolution view. No code changes.

**Related:** [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md) | [service_sprawl_watch.md](service_sprawl_watch.md) | [controller_sprawl_watch.md](controller_sprawl_watch.md) | [migration_integrity_watch.md](../operations/migration_integrity_watch.md) | [system_evolution_risk.md](../operations/system_evolution_risk.md)

---

## 1. Executive summary

**Current architecture health: Stable with known structural risk.** No new drift detected since the last refactor-safety and watchdog pass. The same **P1** hotspots remain: **OrderService** (19 deps), **WorkflowEngineService** (11 deps), **BackgroundJobProcessorService** (orchestration sprawl), **SchedulerService** (runtime Workflow resolution + _context.Orders), and **14 controllers** injecting **ApplicationDbContext**. Migration chain is operationally usable under documented conditions. **Recommended next engineering sequence:** (1) Remove DbContext from controllers (per-controller refactor), (2) Document and enforce “no new constructor deps” for OrderService/WorkflowEngineService until split, (3) Migrate remaining legacy job types to JobExecution, (4) Refresh this summary and sprawl watches on major releases.

---

## 2. Current architecture health

| Dimension | Status | Evidence |
|-----------|--------|----------|
| **Domain boundary** | Clean | No Domain → Infrastructure or API references. |
| **Application boundary** | Leaks | ApplicationDbContext in many services; Microsoft.AspNetCore.Http (IFormFile) in Parser/Settings. |
| **API boundary** | Violations | 14 controllers inject ApplicationDbContext. |
| **Service sprawl** | P1 risks | OrderService, WorkflowEngineService, BackgroundJobProcessorService, SchedulerService (see service_sprawl_watch). |
| **Controller sprawl** | Medium on 4 families | Inventory, Orders, Scheduler, Billing (see controller_sprawl_watch). |
| **Migration chain** | Usable | Idempotent repairs; no-Designer manifest; validator and runbook. |
| **Reliability** | Adequate | Lease-based claiming for JobExecution, EventStore, NotificationDispatch; command idempotency; retry and dead-letter where needed. |

---

## 3. Drift detection

- **Since last baseline:** No **new** controllers, workers, or job types; no new GetRequiredService or _context.XXX cross-domain usages beyond those already listed in hidden_dependencies and dependency_leak_watch.
- **Documentation drift:** None; api_surface_summary, background_jobs, and refactor-safety docs align with current code.
- **Refactor-risk regression:** No module became riskier; classifications unchanged.

---

## 4. Major coupling hotspots

| Hotspot | Type | Risk |
|---------|------|------|
| **OrderService** | Hub (19 deps); used by 7+ call sites | P1 – do not add deps; plan split. |
| **WorkflowEngineService** | Hub (11 deps); all status transitions | P1 – central to lifecycle. |
| **SchedulerService ↔ WorkflowEngineService** | Cycle (constructor + runtime resolution) | P1 – make Workflow explicit in constructor. |
| **BackgroundJobProcessorService** | Orchestration (10+ job types via GetRequiredService) | P1 – new jobs via JobExecution. |
| **BuildingService → _context.Orders** | Cross-domain DbContext | P2 |
| **BillingService → _context.Orders, Invoices** | Cross-domain DbContext | P2 |
| **14 controllers → ApplicationDbContext** | API → Infrastructure | P1 – remove. |

---

## 5. Long-term scalability risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| **OrderService / WorkflowEngineService** become untestable or unmaintainable | High if deps grow | Freeze deps; split or facade. |
| **Legacy BackgroundJob** duplicate execution under multi-instance | Medium | Single instance or add lease; prefer JobExecution. |
| **Event store** lag under load | Medium | Tune batch size and poll; consider partitioning by company later. |
| **Schema drift** (snapshot vs model) | Medium over time | Governance; sync migration when needed. |
| **Multi-tenant settings** | Low for now | Key namespacing or per-company table when required. |

---

## 6. “Looks fine now but will hurt later” areas

- **Controllers with DbContext:** Easy to add “one more query” in a controller; entrenches bypass of application layer.
- **Runtime resolution (GetRequiredService):** New engineers may add more; dependency graph stays incomplete.
- **Notification duplicate send:** No application-level idempotency; duplicate rows can cause duplicate SMS/email.
- **Scheduler enqueue overlap:** Two scheduler ticks can enqueue two identical jobs (e.g. PnlRebuild); executors must be idempotent.

---

## 7. Recommended next engineering sequence

1. **Controller DbContext removal:** Introduce or use existing query services (OrderQueryService, InventoryQueryService, etc.); remove ApplicationDbContext from all 14 controllers. Proceed per-controller.
2. **OrderService / WorkflowEngineService:** Do not add constructor dependencies; document “no new deps without ADR or split plan.” Plan facade or split for OrderService.
3. **Job types:** Prefer JobExecution + IJobExecutor for all new background work; document legacy job types in background_jobs.md; consider single instance for BackgroundJobProcessorService or add lease.
4. **SchedulerService:** Replace GetRequiredService&lt;IWorkflowEngineService&gt; with constructor-injected IWorkflowEngineService when refactoring.
5. **Watchdog refresh:** On major release or quarterly, refresh service_sprawl_watch, controller_sprawl_watch, migration_integrity_watch, system_evolution_risk, and this summary; update counts and risk tables.

---

## 8. Index of Level 5 and maturity artifacts

| Artifact | Path |
|----------|------|
| Level 1 – Code & feature integrity | [docs/engineering/level1_code_integrity.md](../engineering/level1_code_integrity.md) |
| Level 2 – Service dependency graph | [docs/architecture/service_dependency_graph.md](service_dependency_graph.md) |
| Level 2 – Service sprawl analysis | [docs/architecture/service_sprawl_analysis.md](service_sprawl_analysis.md) |
| Level 3 – Architecture integrity audit | [docs/architecture/architecture_integrity_audit.md](architecture_integrity_audit.md) |
| Level 4 – Reliability audit | [docs/operations/reliability_audit.md](../operations/reliability_audit.md) |
| Level 5 – Service sprawl watch | [docs/architecture/service_sprawl_watch.md](service_sprawl_watch.md) |
| Level 5 – Controller sprawl watch | [docs/architecture/controller_sprawl_watch.md](controller_sprawl_watch.md) |
| Level 5 – Migration integrity watch | [docs/operations/migration_integrity_watch.md](../operations/migration_integrity_watch.md) |
| Level 5 – System evolution risk | [docs/operations/system_evolution_risk.md](../operations/system_evolution_risk.md) |
| Architecture watchdog summary | This document |

---

## 10. Engineering Guardrails

Guardrails are in place to prevent the architecture issues identified in the Level 1–5 audit from recurring. They do not refactor existing code; they constrain and detect drift.

### What rules now protect the system

- **Controllers:** Must not inject `ApplicationDbContext` or any Infrastructure type; must call Application services only. Enforced by Cursor rule and by `scripts/architecture/check-controller-boundaries.ps1` (CI warning).
- **Services:** New services must not exceed 12 constructor dependencies or 1,200 LOC; existing oversized services must not grow. Enforced by Cursor rule and by `scripts/architecture/check-service-sprawl.ps1` (CI warning).
- **Domain:** Must not reference Infrastructure or EF Core. Enforced by Cursor rule.
- **Circular dependencies:** Must not introduce new cycles (e.g. A → B → A); prefer constructor injection over `GetRequiredService`. Enforced by Cursor rule.
- **Background jobs:** Must be idempotent; new jobs prefer JobExecution + IJobExecutor. Enforced by Cursor rule.
- **Migrations:** Append-only; snapshot must match chain; no manual schema edits; scripts must pass `validate-migration-hygiene.ps1`. Enforced by [migration_governance.md](../operations/migration_governance.md) and existing migration-hygiene CI (fail on violation).

Full rule set: [architecture_guardrails.md](architecture_guardrails.md). Cursor enforcement: [.cursor/rules/architecture-guardrails.mdc](../../.cursor/rules/architecture-guardrails.mdc).

### How future engineers should follow them

1. **Before adding a controller action:** Use only Application services; do not add or use `ApplicationDbContext` in the controller.
2. **Before adding a new Application service:** Keep constructor dependencies ≤12 and file size ≤1,200 LOC; if the change would exceed, split or extract first.
3. **Before adding a background job:** Prefer JobExecution + IJobExecutor; document idempotency in `docs/operations/background_jobs.md`.
4. **Before adding a migration:** Use `backend/scripts/create-migration.ps1 -MigrationName "DescriptiveName"`; run `backend/scripts/validate-migration-hygiene.ps1` before merging.
5. **In Cursor/IDE:** The architecture-guardrails rule applies when editing backend .cs files; follow the 10 rules in the rule file when generating or reviewing code.

### Where automated checks run

| Check | Where | Effect |
|-------|--------|--------|
| Service sprawl | `.github/workflows/architecture-guardrails.yml` (on Application or script changes) | **Warn** in CI log; does not block. |
| Controller boundaries | Same workflow (on Api Controllers or script changes) | **Warn** in CI log; does not block. |
| Migration hygiene | `.github/workflows/migration-hygiene.yml` (on Migrations changes) | **Fail** if validator fails; blocks merge. |
| Cursor rules | Local / Cursor IDE when editing backend .cs | Guidance and enforcement during development. |

To run guardrail scripts locally (from repo root):

- `pwsh -File scripts/architecture/check-service-sprawl.ps1`
- `pwsh -File scripts/architecture/check-controller-boundaries.ps1`

---

## 11. Related reports

- [ARCHITECTURE_WATCHDOG_REPORT](../ARCHITECTURE_WATCHDOG_REPORT.md) – Detailed drift, sprawl, and worker coupling from Level 15 pass.
- [REFACTOR_SAFETY_REPORT](../REFACTOR_SAFETY_REPORT.md) – Safe/danger zones, refactor sequence.
- [CODEBASE_INTELLIGENCE_MAP](CODEBASE_INTELLIGENCE_MAP.md) – Modules, controllers, services, entities, workers.
- [architecture_guardrails.md](architecture_guardrails.md) – Full guardrails document.
- [migration_governance.md](../operations/migration_governance.md) – Migration rules and integrity checks.
