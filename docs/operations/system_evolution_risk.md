# System Evolution Risk

**Date:** March 2026  
**Purpose:** Long-term platform risk areas, scaling constraints, operational bottlenecks, and recommended control documents. Analysis only; no code changes.

**Related:** [architecture_watchdog_summary.md](../architecture/architecture_watchdog_summary.md) | [reliability_audit.md](reliability_audit.md) | [service_sprawl_analysis.md](../architecture/service_sprawl_analysis.md)

---

## 1. Executive summary

CephasOps is a **single-company/multi-tenant capable** fibre/GPON contractor operations platform. Evolution risks center on **service sprawl** (OrderService, WorkflowEngineService, BackgroundJobProcessorService), **controller–DbContext coupling**, **single-company assumptions** in data model and scope, **background job scaling** (legacy table vs JobExecution), and **module ownership** clarity. Operational bottlenecks: **workflow engine** as single point for all transitions; **scheduler–workflow cycle**; **event store** and **notification dispatch** at-least-once delivery. Recommended control documents for future teams: engineering maturity docs (Level 1–5), migration governance, refactor safety report, and watchdog refresh on each major release.

---

## 2. Long-term platform risk areas

| Area | Risk | Impact |
|------|------|--------|
| **OrderService (19 deps, ~3k LOC)** | Any new integration adds constructor dep; testing and deployment complexity grow. | Regression risk; long build/test times; hard to reason about order lifecycle. |
| **WorkflowEngineService (11 deps)** | Central to all status transitions; new guard/side-effect types increase coupling. | Single point of failure; broad blast radius for bugs. |
| **Controllers with DbContext (14)** | Bypass application layer; duplicate query logic; harder to change persistence. | Architecture drift; inconsistent authorization/caching. |
| **Legacy BackgroundJob vs JobExecution** | Two job systems; legacy has no FOR UPDATE SKIP LOCKED; duplicate run risk if multi-instance. | Confusion; duplicate work; operational incidents. |
| **Scheduler ↔ Workflow cycle** | SchedulerService resolves IWorkflowEngineService at runtime; WorkflowEngineService injects ISchedulerService. | Refactor difficulty; hidden dependencies. |
| **Application → Infrastructure (DbContext)** | Many Application services depend on ApplicationDbContext directly. | Ideal Clean Architecture violated; gradual move to I*Store recommended. |

---

## 3. Single-company assumptions that will resist future scale

| Assumption | Where | Multi-tenant / scale concern |
|------------|--------|------------------------------|
| **CompanyId in scope** | Most queries and services assume company context. | Already multi-company capable; ensure all new features respect CompanyId and department scope. |
| **Single event store** | One EventStore table; correlation by CompanyId. | Partitioning or sharding by company may be needed at very high volume. |
| **In-process workers** | All hosted services in one process. | Horizontal scaling of API vs workers may require separate job worker deployment and lease discipline. |
| **GlobalSettings key-value** | Single table; keys like MyInvois_Enabled. | Per-company or per-tenant settings may require key namespacing or separate table. |
| **Workflow definitions** | Partner/Department/OrderType scoped; one active definition per scope. | Already scoped; evolution to many tenants may need rate limits and isolation. |

---

## 4. Operational bottlenecks

| Bottleneck | Symptom | Mitigation |
|------------|----------|------------|
| **Workflow transition throughput** | All transitions go through WorkflowEngineService; heavy load on one service. | Scale out API; consider async transition queue if needed. |
| **Event store dispatch** | Single EventStoreDispatcherHostedService; batch size and poll interval tune throughput. | Increase batch size or add workers with same lease semantics; monitor lag. |
| **Document generation** | DocumentGenerationService is large and synchronous per request. | Already offloaded to DocumentGenerationJobExecutor; ensure job worker capacity. |
| **P&L rebuild** | PnlRebuild job for period; large companies may have long run. | Chunk by sub-period or entity; document timeout and retry. |
| **MyInvois status poll** | One job per submission or batch; rate limits by LHDN. | Respect rate limits; backoff on 429; document in runbook. |

---

## 5. Hidden architectural liabilities

| Liability | Description | When it hurts |
|-----------|--------------|----------------|
| **Runtime service resolution** | SchedulerService, BackgroundJobProcessorService use GetRequiredService for key dependencies. | Dependency graphs and tests miss these; refactors can break at runtime. |
| **Cross-domain DbContext** | BuildingService, BillingService, SchedulerService query _context.Orders or _context.Invoices. | Order/Invoice schema changes can break multiple modules. |
| **No global idempotency for notifications** | NotificationDispatch rows; duplicate “same” notification possible if two rows created. | Duplicate SMS/email to customer; support load. |
| **Partial transaction risk** | Some multi-step writes use multiple SaveChangesAsync without single transaction. | Inconsistent state on failure; reconciliation jobs needed. |

---

## 6. Recommended control documents for future teams

| Document | Purpose |
|----------|---------|
| **Engineering maturity (Level 1–5)** | [level1_code_integrity](../engineering/level1_code_integrity.md), [service_dependency_graph](../architecture/service_dependency_graph.md), [service_sprawl_analysis](../architecture/service_sprawl_analysis.md), [architecture_integrity_audit](../architecture/architecture_integrity_audit.md), [reliability_audit](reliability_audit.md), watchdog set. |
| **Migration governance** | [EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION](EF_MIGRATION_OPERATIONAL_CLOSURE_DECISION.md), [validate-migration-hygiene.ps1](../../backend/scripts/validate-migration-hygiene.ps1), [create-migration.ps1](../../backend/scripts/create-migration.ps1), [ef-migration-governance rule](../../.cursor/rules/ef-migration-governance.mdc). |
| **Refactor safety** | [REFACTOR_SAFETY_REPORT](../REFACTOR_SAFETY_REPORT.md), [high_coupling_modules](../architecture/high_coupling_modules.md), [hidden_dependencies](../architecture/hidden_dependencies.md), [refactor_sequence_plan](../architecture/refactor_sequence_plan.md). |
| **Watchdog refresh** | On each major release or quarterly: refresh [service_sprawl_watch](../architecture/service_sprawl_watch.md), [controller_sprawl_watch](../architecture/controller_sprawl_watch.md), [migration_integrity_watch](migration_integrity_watch.md), [architecture_watchdog_summary](../architecture/architecture_watchdog_summary.md); update counts and risk tables. |
| **Operations** | [background_jobs](background_jobs.md), [myinvois_production_runbook](myinvois_production_runbook.md), [inventory_ledger_ops_runbook](inventory_ledger_ops_runbook.md), [reporting_pnl_ops_runbook](reporting_pnl_ops_runbook.md). |

---

## 7. Related artifacts

- [Architecture watchdog summary](../architecture/architecture_watchdog_summary.md)
- [Reliability audit](reliability_audit.md)
- [Service sprawl analysis](../architecture/service_sprawl_analysis.md)
- [Level 1 code integrity](../engineering/level1_code_integrity.md)
