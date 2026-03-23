# Safe Refactor Zones

**Related:** [refactor_danger_zones.md](refactor_danger_zones.md) | [refactor_sequence_plan.md](refactor_sequence_plan.md) | [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md)

**Purpose:** Identify areas where refactoring is lower-risk: limited coupling, read-heavy or configuration-only, and non–critical path. Still require tests and change control; “safe” is relative.

---

## Zones considered safer to refactor

| Zone | Why it is relatively safe | Caveats |
|------|----------------------------|---------|
| **Report generation (run by key)** | Read-only aggregation; ReportsController uses IOrderService, IStockLedgerService, ISchedulerService for queries only. No state transitions. New report keys or export formats are additive. | Do not change contracts of underlying services (e.g. breaking method signatures used by reports). InventoryReportExport job runs in BackgroundJobProcessorService—ensure job payload and handler stay in sync. |
| **Settings / reference data (CRUD)** | OrderTypes, BuildingTypes, DocumentTemplates, EmailTemplates, KpiProfiles, GlobalSettings, etc. Mostly CRUD and lookup; many small controllers and services. No order lifecycle or financial logic inside. | Workflow definitions, guard conditions, and side-effect definitions drive WorkflowEngineService—changes there affect transitions. Keep workflow config changes gated and tested. |
| **Admin UI / user management** | AdminUsersController, AdminRolesController, AuthController, AdminSecuritySessionsController. Auth and user CRUD; department membership. Isolated from order/billing/inventory flows. | Password reset, session revoke, and RBAC must remain consistent; avoid changing 403/401 behavior without tests. |
| **Non-critical background jobs** | EmailCleanupService (mail viewer TTL), NotificationRetention (cleanup old notifications). Not on critical path for order or billing. | EmailCleanupService is registered as both Scoped and HostedService in some setups—documented in _discrepancies; fix registration before heavy refactor. |
| **Tasks (Kanban)** | TasksController and task services. Department-scoped but not part of Email→Order→Docket→Invoice flow. | Verify department scope and any links to orders/tickets if they exist. |
| **Assets** | AssetsController; depreciation and maintenance. Not part of core operational flow. | Check for any links to Orders or Billing before large changes. |
| **RMA (request tracking)** | RMAController; request and item tracking. Linked to Inventory but bounded. | RMA lifecycle and inventory return paths; keep ledger/return logic consistent. |
| **Files / Documents (non–OneDrive)** | File and document CRUD; optional OneDrive sync. Not in critical path. | OneDrive sync (IOneDriveSyncService) and parser snapshot sync—ensure file entity and sync status stay consistent if refactoring. |
| **Messaging templates (SMS/WhatsApp)** | Template CRUD and rendering; provider selection (Twilio, Cloud API, null). Sending is in NotificationDispatchWorker. | Template codes and placeholders are contract for senders; avoid breaking template resolution. |
| **Diagnostics / health / observability** | AdminController, DiagnosticsController, InfrastructureController, SystemWorkersController, SystemSchedulerController. Read-only status and heartbeat. | Do not change worker identity or scheduler status contracts if other systems depend on them. |
| **Event bus metrics** | EventBusMetricsCollectorHostedService; throughput/lag metrics. Observability only. | Ensure it does not alter event store or dispatcher behavior. |

---

## Why these are “safe”

- **Read-heavy or config-only:** No order status changes, no invoice creation, no ledger writes, no payout calculation.
- **Limited coupling:** Either few callers or callers only read data.
- **Non–critical path:** Failure or delay does not block Email→Order→Schedule→Field→Docket→Invoice→Payment.
- **Additive changes:** New report keys, new settings entities, or new admin features are lower risk than changing existing workflow or billing logic.

---

**Refresh:** When a “safe” zone gains dependencies on Orders, Billing, Workflow, or Inventory (e.g. new validations or side effects), reclassify in [refactor_danger_zones.md](refactor_danger_zones.md) or [high_coupling_modules.md](high_coupling_modules.md).
