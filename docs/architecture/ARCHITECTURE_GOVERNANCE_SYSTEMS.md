# Architecture Governance Systems

**Status:** Active  
**Purpose:** Index of named architecture governance systems in CephasOps. Each system is implemented by documented artifacts and processes; this page is the single reference for "what exists" and where to find it.

---

## 1. Change Impact Predictor

**Exists.** Predicts impact of code or refactor changes across modules and dependencies.

| Artifact | Role |
|----------|------|
| [refactor_sequence_plan.md](refactor_sequence_plan.md) | Safe order of refactoring (least → most critical); impact order. |
| [module_dependency_map.md](module_dependency_map.md) | Upstream/downstream per module; dependency direction. |
| [hidden_dependencies.md](hidden_dependencies.md) | Service→service and cross-domain DbContext; runtime resolution. |
| [dependency_leak_watch.md](dependency_leak_watch.md) | Leak register and cycle risk; who is affected by a change. |

**Use when:** Planning a refactor, adding a new service dependency, or assessing "what breaks if I change X."

---

## 2. Architecture Policy Engine

**Exists.** Encodes architecture policies: coupling limits, danger zones, safe zones, and boundary rules.

| Artifact | Role |
|----------|------|
| [high_coupling_modules.md](high_coupling_modules.md) | Modules ranked by coupling; policy-level "do not add more deps" for P1 services. |
| [refactor_danger_zones.md](refactor_danger_zones.md) | High-risk areas and mitigation (order lifecycle, billing, ledger, event store, etc.). |
| [safe_refactor_zones.md](safe_refactor_zones.md) | Lower-risk areas where changes are allowed with less ceremony. |
| [module_boundary_regression.md](module_boundary_regression.md) | Boundary status per module (stable / drifting); policy to avoid further drift. |
| [service_sprawl_watch.md](service_sprawl_watch.md) | Sprawl risk and monitoring priority (P1/P2) for services. |

**Use when:** Deciding where to allow changes, what needs extra tests, or whether a change violates a boundary.

---

## 3. Auto Documentation Sync System

**Exists.** Keeps documentation aligned with code and with each other via triggers, checklists, and logs.

| Artifact | Role |
|----------|------|
| [CODEBASE_INTELLIGENCE_MAP.md](CODEBASE_INTELLIGENCE_MAP.md) | **Refresh triggers** (§ bottom): when to update maps (new controllers, services, workers, integrations, workflow, RBAC). |
| [DOCUMENTATION_ALIGNMENT_CHECKLIST.md](../DOCUMENTATION_ALIGNMENT_CHECKLIST.md) | Required doc set (A–P) and completion; links to source-of-truth docs. |
| [DOCS_MAP.md](../DOCS_MAP.md) | Required doc set; canonical vs reference. |
| [DOCS_STATUS.md](../DOCS_STATUS.md) | Doc status and link-fix history; last audit dates. |
| [CHANGELOG_DOCS.md](../CHANGELOG_DOCS.md) | Log of doc changes (created/updated) and why. |
| [_discrepancies.md](../_discrepancies.md) | Code vs docs mismatches; accepted gaps; deferred. |

**Use when:** Running a doc sync pass, onboarding, or verifying that docs are up to date after a release.

---

## 4. Architecture Risk Dashboard

**Exists.** Single place to see overall architecture risk, drift, and hotspots.

| Artifact | Role |
|----------|------|
| [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md) | Executive summary; high-coupling summary; hidden deps; fragile modules; safe/danger zones; worker risks; strategy. |
| [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md) | Periodic health; drift trend; service/controller sprawl; dependency leaks; worker coupling; module boundaries; refactor risk change. |
| [module_fragility_map.md](module_fragility_map.md) | Per-module fragility (size, coupling, worker usage, criticality). |
| [worker_dependency_risks.md](worker_dependency_risks.md) | Worker→service dependencies and hidden coupling. |

**Use when:** Leadership or architects need a quick risk view; preparing for an architecture review.

---

## 5. Self-Maintaining Architecture System

**Exists.** Rules and triggers that keep the architecture and doc layer updated when the codebase changes.

| Artifact | Role |
|----------|------|
| [CODEBASE_INTELLIGENCE_MAP.md](CODEBASE_INTELLIGENCE_MAP.md) | **Refresh triggers:** new controller families; new domain services or workers; new integrations; workflow or eventing changes; RBAC changes → update map and linked maps; run architecture audit if needed. |
| [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md) | **Suggested next architecture actions** and **Watchdog trigger rules:** when to re-run watchdog (new controllers, workers, job types, GetRequiredService or _context.XXX cross-domain, changes to OrderService/WorkflowEngineService/SchedulerService/BillingService). |
| [service_sprawl_watch.md](service_sprawl_watch.md), [controller_sprawl_watch.md](controller_sprawl_watch.md), [dependency_leak_watch.md](dependency_leak_watch.md), [worker_coupling_watch.md](worker_coupling_watch.md), [module_boundary_regression.md](module_boundary_regression.md) | **Refresh** instructions at bottom of each: when to re-scan and update. |

**Use when:** Defining "when do we refresh the architecture docs" or automating a periodic watchdog run.

---

## 6. Portal Navigation

**Updated.** Single entry point and quick links to all governance and architecture docs.

| Artifact | Role |
|----------|------|
| [README.md](../README.md) | Main docs index; navigation line to DOCS_MAP, ARCHITECTURE_AUDIT_REPORT, CODEBASE_INTELLIGENCE_MAP, CODEBASE_INTELLIGENCE_REPORT, REFACTOR_SAFETY_REPORT, ARCHITECTURE_WATCHDOG_REPORT. |
| [00_QUICK_NAVIGATION.md](../00_QUICK_NAVIGATION.md) | Quick links by topic: source-of-truth, codebase intelligence, refactor safety, architecture watchdog, operations, getting started. |
| [_INDEX.md](../_INDEX.md) | Full index: source-of-truth table, refactor safety table, architecture watchdog table, codebase intelligence table, by folder. |
| [architecture/README.md](README.md) | Architecture folder index: diagrams, codebase intelligence, refactor safety, watchdog. |

---

## 7. Governance Logs

**Updated.** Audit trail and status for docs and architecture.

| Artifact | Role |
|----------|------|
| [CHANGELOG_DOCS.md](../CHANGELOG_DOCS.md) | Log of doc creation/update by pass (reorg, living-docs, architecture audit, codebase intelligence, refactor safety, watchdog). |
| [DOCS_STATUS.md](../DOCS_STATUS.md) | Last audit date; link and path fixes; known gaps; completed vs doc. |
| [_discrepancies.md](../_discrepancies.md) | Code vs docs mismatches; closed/open/accepted/deferred. |
| [DOCUMENTATION_ALIGNMENT_CHECKLIST.md](../DOCUMENTATION_ALIGNMENT_CHECKLIST.md) | Checklist of required docs and governance artifacts (A–P, audit report, intelligence, refactor safety, watchdog). |

---

**Summary:** Change Impact Predictor, Architecture Policy Engine, Auto Documentation Sync, Architecture Risk Dashboard, and Self-Maintaining Architecture are implemented by the listed artifacts. Portal navigation and governance logs are maintained in the listed files and are updated when new architecture or governance docs are added.
