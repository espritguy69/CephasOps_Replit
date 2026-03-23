# Operational Observability — Implementation Report

**Date:** 2026-03-12  
**Scope:** Internal operational visibility layer so operators can see whether core platform safety mechanisms are healthy and where failures occur. No schema changes, no migrations, no BI or customer-facing analytics. Backend/API only; reuses existing data sources and admin patterns.

---

## 1. What operational observability capability was added

- **Operational overview API**  
  A single read-only endpoint that returns a compact JSON summary of the most important operational signals:

  - **Job executions** — Pending, running, failed (retry scheduled), dead-letter, and succeeded counts from the existing job execution queue.
  - **Event store (last 24h)** — Events in window, processed/failed/dead-letter counts and percentages, top 5 failing event types, top 5 failing companies (by failed + dead-letter).
  - **Payout health** — Snapshot coverage (completed with vs missing snapshot, coverage %), anomaly counts (legacy fallback, zero payout, negative margin), and latest repair run (id, started/completed, total/created/error counts, trigger source).
  - **System health** — Database connected flag and background job runner status (from existing admin health).
  - **Guard violations** — Recent platform safety guard violations (in-memory buffer): total recorded, counts by guard name (e.g. TenantSafetyGuard, FinancialIsolationGuard, EventStoreConsistencyGuard), and up to 50 most recent items (timestamp, guard name, operation, short message, optional CompanyId/EntityType/EntityId/EventId). Process restart clears the buffer. This is read-only observability; it does not replace log inspection for deep forensics.

- **Operations overview service**  
  A small application-layer service that aggregates the above from existing services only (no new persistence, no new tables). It runs the four underlying calls in parallel and maps results into a single DTO.

- **Control-plane discovery**  
  The operations overview is listed in the admin control-plane capabilities so operators can discover the endpoint (`/api/admin/operations/overview`) from the existing admin surface.

---

## 2. Which data sources / services / endpoints were used or extended

| Source | Use |
|--------|-----|
| **IJobExecutionQueryService.GetSummaryAsync** | Job execution counts (pending, running, failed retry, dead-letter, succeeded). |
| **IEventStoreQueryService.GetDashboardAsync(from, to, scopeCompanyId: null)** | Event-store metrics for a 24h window: events in window, processed/failed/dead-letter counts and percentages, top failing event types and companies. |
| **IPayoutHealthDashboardService.GetDashboardAsync** | Snapshot health, anomaly summary, latest repair run. |
| **IAdminService.GetHealthAsync** | Database connected, background job runner status. |
| **ControlPlaneController** | Extended with one capability entry: "Operations overview" → `/api/admin/operations/overview`. |
| **New: OperationsOverviewController** | `GET api/admin/operations/overview` — returns `OperationalOverviewDto`. |
| **New: IOperationsOverviewService / OperationsOverviewService** | Aggregates the four services above into `OperationalOverviewDto`. |
| **New: OperationalOverviewDto** (and nested DTOs) | Compact shape for the overview response; no new persisted data. |

No database tables or migrations were added. All data comes from existing query services and their underlying persisted data (JobExecutions, event store tables, payout snapshots, repair runs, admin health checks).

---

## 3. What failures or health signals are now visible

- **Job queue** — Failed (retry scheduled) and dead-letter counts; pending and running; succeeded. Operators can see if jobs are piling up or repeatedly failing.
- **Event store** — In the last 24h: how many events were processed vs failed vs dead-letter; percentages; which event types and which companies (top 5 each) account for most failures. Supports detection of EventStore consistency or handler issues.
- **Payout / billing** — Snapshot coverage (completed orders with vs without snapshot); legacy fallback, zero payout, and negative margin anomaly counts; latest snapshot repair run (errors, created, trigger). Supports detection of payout/snapshot/repair issues.
- **System** — Database connectivity and background job runner status (from existing health). No new health checks were added.

**Guard violations (in-memory)**  
There is no persisted “guard violation count” (tenant safety, financial isolation, or EventStore consistency guard). The overview does not fabricate these; it only surfaces what is already observable (job executions, event store dashboard, payout health, system health). The overview now includes guard violations from an in-memory buffer (see Section 1); for full history use logs with category "PlatformGuardViolation".

---

## 4. What tests were added or updated

**New tests** — **OperationsOverviewServiceTests** (`CephasOps.Application.Tests/Admin/OperationsOverviewServiceTests.cs`):

- **GetOverviewAsync_ReturnsExpectedShape_WithAllSections**  
  Mocks the four dependencies with non-empty data; asserts the returned `OperationalOverviewDto` has the expected structure and that job, event store, payout, and system health values match the mocked data (counts, top lists, repair run, database/runner status); asserts GuardViolations section is present.

- **GetOverviewAsync_EmptyData_ReturnsValidShape_NoThrow**  
  All mocks return zero/empty/null; asserts the overview is non-null, all sections are present, counts are zero, lists are empty, `LatestRepairRun` is null, system health reflects disconnected/unknown where applicable, and GuardViolations is empty. Ensures empty-state behavior is safe and consistent.

- **GetOverviewAsync_WhenGuardViolationBufferHasEntries_ReturnsThemInGuardViolationsSection**  
  Injects a test buffer with three violations (two TenantSafetyGuard, one FinancialIsolationGuard); asserts GuardViolations.TotalRecorded, ByGuard counts, and Recent list shape. Ensures guard violation summary logic groups and returns data correctly.

- **GetOverviewAsync_CapsTopFailingLists_AtFive**  
  Event store mock returns more than 5 top failing event types and more than 5 top failing companies; asserts the overview’s `TopFailingEventTypes` and `TopFailingCompanies` each have exactly 5 items. Ensures aggregation caps are applied.

No existing tests were modified. No API integration tests were added (endpoint is covered by the same auth pattern as other admin endpoints; manual or existing auth tests apply).

---

## 5. Assumptions or unresolved limitations

- **Guard violation counts** — Tenant safety, financial isolation, and EventStore consistency guards do not currently write to a shared “violation count” table. The overview does not show guard-trigger counts; it shows job failures, event store failures, and payout health from existing sources. Adding guard-trigger visibility would require a separate, small decision (e.g. categorised log or existing operational table) and no schema change was made in this pass.

- **Time window** — Event store section uses a fixed 24h window (window end = now). The overview does not support configurable windows; that could be added later if needed.

- **Authorization** — The overview endpoint uses the same pattern as the rest of the admin control plane: `[Authorize(Roles = "SuperAdmin,Admin")]` and `[RequirePermission(PermissionCatalog.JobsView)]`. No new permission was introduced.

- **No UI** — Per requirements, no frontend or dashboard UI was added; the deliverable is the backend API and service. Existing admin UIs can call the new endpoint if desired.

---

## 6. Why this is safe and does not change valid business behavior

- **Read-only** — The overview service and endpoint only read from existing query services. No writes, no new persistence, no changes to job execution, event store, or payout logic.
- **No schema changes** — No migrations, no new tables, no new columns. All data comes from existing structures.
- **Reuse of existing patterns** — Same auth and permission model as other admin endpoints; same style of DTO and controller as existing admin/health and payout-health surfaces.
- **Bounded scope** — The overview aggregates already-available data into a single response. It does not introduce new business rules, new workflows, or new side effects. Failures in the overview (e.g. one of the four underlying calls failing) would result in an error response for the overview call only; they do not affect tenant isolation, financial guards, or EventStore consistency.
- **No weakening of safeguards** — Tenant, financial, and EventStore guards are untouched. This pass only adds a visibility layer on top of existing operational data.

---

**Summary:** A small operational observability layer was added: one new endpoint (`GET api/admin/operations/overview`), one new application service that aggregates four existing services, and a compact DTO. Operators can see job execution state, event store health (last 24h), payout/snapshot and repair health, and system health in one place. Tests cover shape, empty state, and aggregation caps. The implementation is production-safe, uses only existing data sources, and does not change business behavior or schema.
