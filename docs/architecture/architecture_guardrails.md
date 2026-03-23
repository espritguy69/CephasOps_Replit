# Architecture Guardrails

**Date:** March 2026  
**Purpose:** Permanent guardrails to prevent architectural drift and enforce the engineering maturity model. These rules protect the system from the issues identified in the Level 1–5 audit. Governance and automation only; no production code refactors.

**Related:** [architecture_integrity_audit.md](architecture_integrity_audit.md) | [level1_code_integrity.md](../engineering/level1_code_integrity.md) | [architecture_watchdog_summary.md](architecture_watchdog_summary.md)

---

## 1. Purpose of guardrails

- **Prevent** controllers from accessing DbContext or Infrastructure directly.
- **Control** service sprawl (size and constructor dependency count).
- **Keep** Clean Architecture boundaries (Domain / Application / Infrastructure / API) intact.
- **Keep** migrations append-only and safe; no silent schema drift.
- **Detect** circular dependencies and new boundary violations before merge.
- **Guide** future developers to follow the intended architecture via Cursor rules, scripts, and CI warnings.

Guardrails are enforced through: (1) documentation (this document and migration_governance), (2) Cursor rules (`.cursor/rules/architecture-guardrails.mdc`), (3) PowerShell scripts (`scripts/architecture/check-service-sprawl.ps1`, `scripts/architecture/check-controller-boundaries.ps1`), (4) CI workflow (warnings only, non-blocking).

---

## 2. Architecture boundaries

| Layer | May reference | Must not reference |
|-------|----------------|---------------------|
| **Domain** | Nothing (or minimal shared kernel) | Application, Infrastructure, API, EF Core, Microsoft.AspNetCore |
| **Application** | Domain | API (CephasOps.Api); avoid Infrastructure types (prefer Domain interfaces); avoid AspNetCore except IFormFile where already used |
| **Infrastructure** | Domain, Application (interfaces) | API (except DI registration in host) |
| **API** | Application, Domain (DTOs), Infrastructure (DI only in Program.cs) | **DbContext in controllers**; business logic in controllers |

**Key rule:** Controllers must not inject or use `ApplicationDbContext` or any `CephasOps.Infrastructure` type. All data access and orchestration go through Application services.

---

## 3. Forbidden dependency patterns

- **API → Infrastructure in controller code:** Controllers must not have `ApplicationDbContext`, `EventStoreRepository`, or any Infrastructure implementation as a constructor parameter or field.
- **Domain → Infrastructure:** No `using CephasOps.Infrastructure` or EF Core in Domain.
- **Domain → API:** No `using CephasOps.Api` or `Microsoft.AspNetCore` in Domain.
- **Application → API:** No references to CephasOps.Api in Application.
- **Circular service dependencies:** Avoid A → B → A (e.g. WorkflowEngineService ↔ SchedulerService). Prefer explicit constructor injection over `GetRequiredService` to make cycles visible.

---

## 4. Controller rules

1. **Do not inject DbContext.** Use Application services (e.g. IOrderService, IOrderQueryService) for all reads and writes.
2. **Do not inject Infrastructure services directly.** Controllers depend on Application-layer interfaces only.
3. **Do not implement business logic.** Controllers orchestrate: validate input, call one or more Application services, map results to API DTOs, return status codes.
4. **Keep controllers thin.** Prefer many small controllers over one controller with 20+ actions and mixed concerns.
5. **New endpoints:** When adding actions, inject an Application service; do not add a new `ApplicationDbContext` or `_context` field.

---

## 5. Service size guidelines

- **Constructor dependencies:** No new Application service should exceed **12** constructor parameters. Existing services above 12 (e.g. OrderService with 19) must not add more; plan a split or facade before adding dependencies.
- **Lines of code:** No new Application service file should exceed **1,200** LOC. Existing files above 1,200 (e.g. DocumentGenerationService ~1,388) are grandfathered but should be split when touched.
- **Orchestration sprawl:** Avoid adding new job types to `BackgroundJobProcessorService`; use **JobExecution** + **IJobExecutor** for new background work. Avoid new `GetRequiredService<T>` in core services; prefer constructor injection.

Automated check: `scripts/architecture/check-service-sprawl.ps1` warns on services >1,200 LOC or >12 constructor dependencies.

---

## 6. Background job rules

- **Idempotency:** All job executors (IJobExecutor) and legacy job handlers must be idempotent by payload (e.g. PnlRebuild for period X; ReconcileLedgerBalanceCache for scope). Document idempotency in `docs/operations/background_jobs.md` when adding a job type.
- **New jobs:** Prefer **JobExecution** + **IJobExecutor** and register in `IJobExecutorRegistry`. Do not add new job types to the legacy `BackgroundJobProcessorService` switch without ADR.
- **Workflow transitions from jobs:** Jobs may call `IWorkflowEngineService`; they must not call controllers or HTTP endpoints for transitions.
- **Lease and claim:** JobExecution and EventStore use lease-based claiming; do not bypass or duplicate this pattern for new workers.

---

## 7. Migration governance

- **Append-only:** Do not modify or delete existing migration files. New changes = new migration.
- **Snapshot:** `ApplicationDbContextModelSnapshot.cs` must match the migration chain. Do not edit the snapshot unless the task is explicit snapshot reconciliation.
- **No manual schema edits:** Schema changes go through EF migrations (or documented script-only migrations in the manifest). See [migration_governance.md](../operations/migration_governance.md) and [migration_integrity_watch.md](../operations/migration_integrity_watch.md).
- **Integrity checks:** Run `backend/scripts/validate-migration-hygiene.ps1` before merging migration changes. Create migrations with `backend/scripts/create-migration.ps1 -MigrationName "DescriptiveName"`.

---

## 8. Integration safety rules

- **External outbound (e.g. MyInvois):** Use submission history and idempotency where applicable; do not resubmit without checking "already submitted" when required.
- **Inbound webhooks:** Use `IExternalIdempotencyStore` (or equivalent) to deduplicate by external idempotency key.
- **Workflow transitions:** Must be triggered via Application services (e.g. WorkflowEngineService), not by controllers calling DbContext or by jobs calling HTTP back into the API.

---

## 9. Refactor safety guidelines

- **Do not** add a new constructor dependency to **OrderService** or **WorkflowEngineService** without an ADR or split plan (see [service_sprawl_analysis.md](service_sprawl_analysis.md)).
- **Do not** add `ApplicationDbContext` to a controller; add or use an Application query/command service instead.
- **Do not** introduce new circular dependencies (e.g. Service A → B → A); use constructor injection so cycles are visible at compile time.
- **Safe refactors:** Adding new Application services with ≤12 deps and ≤1,200 LOC; adding new controllers that only call Application services; adding new IJobExecutor implementations; extending Domain with new entities and interfaces implemented in Infrastructure.

---

## 10. Engineering maturity model summary

| Level | Focus | Key artifacts |
|-------|--------|----------------|
| **Level 1** | Code & feature integrity | [level1_code_integrity.md](../engineering/level1_code_integrity.md) – controllers vs services, validation, transactions, job safety |
| **Level 2** | Service & module design | [service_dependency_graph.md](service_dependency_graph.md), [service_sprawl_analysis.md](service_sprawl_analysis.md) – central services, P1/P2/P3 ranking |
| **Level 3** | Architecture integrity | [architecture_integrity_audit.md](architecture_integrity_audit.md) – Clean Architecture violations, boundary leaks |
| **Level 4** | Reliability & production safety | [reliability_audit.md](../operations/reliability_audit.md) – jobs, idempotency, retries, transactions |
| **Level 5** | Observability & evolution | [architecture_watchdog_summary.md](architecture_watchdog_summary.md), sprawl watches, migration integrity, system evolution risk |

Guardrails enforce the findings of these audits so that new code does not reintroduce the same violations (e.g. new controllers with DbContext, new services with >12 deps or >1,200 LOC).

---

## Automated checks

| Check | Script | When | Effect |
|-------|--------|------|--------|
| Service sprawl | `scripts/architecture/check-service-sprawl.ps1` | CI on PR/push (backend Application changes) | **Warn** only |
| Controller boundaries | `scripts/architecture/check-controller-boundaries.ps1` | CI on PR/push (Api Controllers changes) | **Warn** only |
| Migration hygiene | `backend/scripts/validate-migration-hygiene.ps1` | CI on migration file changes | **Fail** (existing) |

See [architecture_watchdog_summary.md § Engineering Guardrails](architecture_watchdog_summary.md#10-engineering-guardrails) for how to follow these rules and where checks run.
