# Architecture Watchdog Report

**Date:** March 2026  
**Pass:** Level 15 – Continuous architecture watchdog  
**Scope:** Drift detection, service/controller sprawl, dependency leaks, worker coupling, module boundary regression, refactor-risk check. Documentation and governance only; no application code changed.

---

## 1. Executive summary

### Overall architecture health

**Stable with known structural risk.** The codebase was compared to the existing intelligence layer (CODEBASE_INTELLIGENCE_MAP, controller_service_map, module_dependency_map, background_worker_map) and refactor safety layer (REFACTOR_SAFETY_REPORT, high_coupling_modules, hidden_dependencies, module_fragility_map, worker_dependency_risks, refactor_danger_zones). No **new** architecture drift was detected since the Level 14 refactor safety audit: controller count (~113 files / ~90+ unique), hosted service count (15), and dependency patterns match the documented baseline.

### Drift trend

- **Architecture drift:** No material increase. Existing drift (OrderService 19 deps, SchedulerService→Workflow runtime resolution, cross-domain DbContext in BuildingService/BillingService/SchedulerService) remains as documented in Level 14.
- **Documentation drift:** None. api_surface_summary, background_jobs, and refactor safety docs align with current code.
- **Refactor-risk regression:** No modules became riskier since Level 14; classifications unchanged.

---

## 2. New drift detected

**None.** All current coupling, hidden dependencies, and boundary issues were already captured in:

- [architecture/high_coupling_modules.md](architecture/high_coupling_modules.md)
- [architecture/hidden_dependencies.md](architecture/hidden_dependencies.md)
- [architecture/module_fragility_map.md](architecture/module_fragility_map.md)
- [architecture/dependency_leak_watch.md](architecture/dependency_leak_watch.md) (new in this pass; consolidates leak view)
- [architecture/module_boundary_regression.md](architecture/module_boundary_regression.md) (new; status per module)

No new controllers, workers, or job types were added. No new GetRequiredService or _context.XXX cross-domain usages were found beyond those already listed.

---

## 3. Service sprawl findings

- **OrderService:** 19 constructor dependencies; ~3000+ LOC; used by 7+ call sites across modules. **Priority P1** – do not add further constructor dependencies without extracting a facade or splitting responsibilities. See [architecture/service_sprawl_watch.md](architecture/service_sprawl_watch.md).
- **WorkflowEngineService:** 11 constructor dependencies; single point for all order status transitions; used by Orders, Scheduler, Billing, Parser, Agent. **P1** – central to lifecycle.
- **BackgroundJobProcessorService:** Orchestration sprawl (one processor, 10+ job types, many GetRequiredService resolutions). **P1** – new job types add hidden coupling; document each in background_jobs.md.
- **SchedulerService:** Large codebase; direct _context.Orders usage; 5× GetRequiredService&lt;IWorkflowEngineService&gt;. **P1** – reduce Order access and make Workflow dependency explicit when refactoring.
- **BillingService, BuildingService, InvoiceSubmissionService:** Medium–high or medium sprawl risk; documented in service_sprawl_watch.

---

## 4. Controller sprawl findings

- **113** controller files under Api/Controllers (unique count ~90+ per api_surface_summary). No single controller family exceeds recommended breadth to "high" sprawl.
- **InventoryController, OrdersController, SchedulerController, BillingController** are the largest (medium risk); already documented in api_surface_summary and controller_sprawl_watch.
- **Settings:** Many small controllers (intentional); aggregate sprawl low–medium.
- **Eventing/operational:** Many focused controllers; documented in api_surface_summary §6.

See [architecture/controller_sprawl_watch.md](architecture/controller_sprawl_watch.md).

---

## 5. Dependency leak findings

- **Runtime resolution:** SchedulerService→IWorkflowEngineService (5×); NoSchedulingConflictsValidator→ISchedulerService; OrderStatusChangedNotificationHandler→IOrderService, IGlobalSettingsService; BackgroundJobProcessorService→many domain services per job type.
- **Cross-domain DbContext:** BuildingService→_context.Orders; BillingService→_context.Orders, _context.Invoices; SchedulerService→_context.Orders.
- **Suspected cycle:** Scheduler ↔ Workflow (Workflow injects Scheduler; Scheduler resolves Workflow at runtime). Documented in dependency_leak_watch.

See [architecture/dependency_leak_watch.md](architecture/dependency_leak_watch.md) and [architecture/hidden_dependencies.md](architecture/hidden_dependencies.md).

---

## 6. Worker coupling findings

- **15** hosted services; no new workers or job types since Level 14.
- **BackgroundJobProcessorService** and **EventStoreDispatcherHostedService** remain high-coupling; **NotificationDispatchWorkerHostedService** and **JobExecutionWorkerHostedService** medium. All others low or low–medium.
- **Risk trend:** Stable. Re-scan when AddHostedService or new job type handlers are added.

See [architecture/worker_coupling_watch.md](architecture/worker_coupling_watch.md) and [architecture/worker_dependency_risks.md](architecture/worker_dependency_risks.md).

---

## 7. Module boundary findings

- **Drifting:** Orders (broad constructor surface), Workflow (universal status dependency), Scheduler (direct Order access + runtime Workflow), Billing (Order/Workflow access), Buildings (Order access for count/merge).
- **Stable:** Inventory (ledger boundary clear), Parser (IOrderService/IBuildingService only), Notifications (handler coupling documented), Settings (many small services), Events (handler set), Rates/Payroll, Reports (read-only consumer).

See [architecture/module_boundary_regression.md](architecture/module_boundary_regression.md).

---

## 8. Refactor risk change

- **No regression.** high_coupling_modules, hidden_dependencies, module_fragility_map, worker_dependency_risks, refactor_danger_zones, and refactor_sequence_plan were **not** updated—current code matches the Level 14 assessment. No module became safer or riskier in this scan.
- If future code changes add controllers, workers, or dependencies, refresh those docs and this section.

---

## 9. Documentation updates made

| Action | File(s) |
|--------|---------|
| **Created** | docs/architecture/service_sprawl_watch.md |
| **Created** | docs/architecture/controller_sprawl_watch.md |
| **Created** | docs/architecture/dependency_leak_watch.md |
| **Created** | docs/architecture/worker_coupling_watch.md |
| **Created** | docs/architecture/module_boundary_regression.md |
| **Created** | docs/ARCHITECTURE_WATCHDOG_REPORT.md (this report) |
| **Updated** | docs/architecture/CODEBASE_INTELLIGENCE_MAP.md – added governance cross-links to watchdog and watch docs |
| **Updated** | docs/_discrepancies.md – added Level 15 watchdog run note (validation date) |
| **Updated** | docs/DOCUMENTATION_ALIGNMENT_CHECKLIST.md – watchdog pass and link to this report |
| **Updated** | docs/DOCS_STATUS.md – Level 15 watchdog bullet |
| **Updated** | docs/README.md, 00_QUICK_NAVIGATION.md, _INDEX.md – links to ARCHITECTURE_WATCHDOG_REPORT and watch docs |
| **Updated** | docs/CHANGELOG_DOCS.md – 2026-03 Architecture watchdog (Level 15) entry |

**Not updated (unchanged):** high_coupling_modules, hidden_dependencies, module_fragility_map, safe_refactor_zones, refactor_danger_zones, refactor_sequence_plan, worker_dependency_risks, CODEBASE_INTELLIGENCE_MAP §1–6 (content), controller_service_map, module_dependency_map, background_worker_map, integration_map, entity_domain_map. No churn.

---

## 10. Remaining concerns

- **OrderService and WorkflowEngineService** remain the highest-sprawl services; any new feature that adds constructor dependencies there should trigger a watchdog refresh and consideration of facades or events.
- **Scheduler–Workflow cycle** (bidirectional dependency) is documented but not refactored; prefer explicit injection and possible facade when touching this area.
- **BackgroundJobProcessorService** job-type → handler → service mapping is not yet a single consolidated doc; recommend a "Job type registry" section in operations/background_jobs.md or a dedicated architecture doc when adding job types.
- **Event handler index** (event type → handler responsibilities) would help event-store and replay governance; optional next step.

---

## 11. Suggested next architecture actions

- **Monitoring focus:** Re-run watchdog when new controller families, new hosted services, new job types, or new GetRequiredService / _context.XXX cross-domain usage appear. Re-scan after any change to OrderService, WorkflowEngineService, SchedulerService, or BillingService constructor or job processor switch.
- **Documentation actions:** (1) Add job-type → handler → service table to operations/background_jobs.md or architecture when new job types are added. (2) Optionally add event-handler index to EVENT_BUS_OPERATIONS_RUNBOOK or architecture. (3) Keep ARCHITECTURE_WATCHDOG_REPORT and the five watch docs updated on each watchdog run.
- **No code changes** recommended in this pass; structural risks are documented and governance is in place.

---

**Definition of done (Level 15):** Architecture drift checked against intelligence layer; service sprawl evaluated; controller sprawl evaluated; dependency leaks evaluated; worker coupling regression evaluated; module boundary regression evaluated; refactor risk marked stable (no updates); ARCHITECTURE_WATCHDOG_REPORT exists; governance and portal links updated.
