# Phase 7: Notification Retention Extraction

## A. Legacy notificationretention flow audited

### Where it was processed
- **Processor**: `BackgroundJobProcessorService.ProcessNotificationRetentionJobAsync`.
- **Payload**: optional `archiveDays` (default 90), `deleteDays` (default 365).

### Retention logic (legacy)
1. **Archive**: Notifications where `Status != "Archived"` and `CreatedAt < archiveCutoff` and `Status in (Read, Unread)` → set `Status = "Archived"`, `ArchivedAt = UtcNow`, `UpdatedAt = UtcNow`.
2. **Delete**: Notifications where `Status == "Archived"` and `ArchivedAt < deleteCutoff` → hard delete (`RemoveRange` + `SaveChanges`).

### Scope and data
- **Scope**: Global. No `CompanyId` filter; all notifications were eligible.
- **Data touched**: Only `Notifications` table. No cleanup of NotificationDispatches, attempt history, or other related data.
- **Producers**: No code path in the codebase creates a BackgroundJob with `JobType = "notificationretention"`. Execution existed only when such a job was present (e.g. manual or external scheduler).

### Safety and risks
- **Over-deletion**: Mitigated by two-stage (archive then delete) and age cutoffs. Default 90/365 days is conservative.
- **Tenant leakage**: Legacy did not scope by company; all tenants’ notifications were processed together. For multi-tenant safety we added optional `companyId` to the new service (when null, global; when set, scoped).
- **Visibility**: Legacy logged archived/deleted counts; no structured audit table.

---

## B. Convergence model chosen

- **Target: Notifications boundary** (not JobExecution).
- **Reason**: Retention is lifecycle maintenance of notification data only; it fits the Notifications boundary. JobExecution is for business workflows (PnL, document generation, etc.). We added `INotificationRetentionService` and a scheduled runner inside the same boundary.
- **Implementation**: (1) `INotificationRetentionService.RunRetentionAsync(archiveDays, deleteDays, companyId?)` performs archive-then-delete with optional company scope. (2) `NotificationRetentionHostedService` runs retention on a configurable interval (default 24h) with configurable default days. (3) Legacy `notificationretention` handler in BackgroundJobProcessorService replaced with a deprecation no-op that marks the job complete without running retention (drain only).

---

## C. Producer/execution paths migrated

- **Execution path**: Retention no longer runs in BackgroundJobProcessorService. It runs via:
  1. **Scheduled**: `NotificationRetentionHostedService` invokes `INotificationRetentionService.RunRetentionAsync` at interval (default every 24 hours) with options `ArchiveDays`/`DeleteDays` (default 90/365), `companyId: null` (global).
  2. **On-demand**: Any caller can inject `INotificationRetentionService` and call `RunRetentionAsync` with custom days and optional `companyId` (e.g. admin API or maintenance script).
- **Legacy producer**: There was no in-code producer for notificationretention jobs; the new “producer” is the hosted service schedule.

---

## D. Legacy BackgroundJob responsibility reduced

- **notificationretention** is no longer executed for retention. `BackgroundJobProcessorService` routes `"notificationretention"` to `ProcessNotificationRetentionJobDeprecatedAsync`, which logs a deprecation warning and returns `true` so the job is marked completed. Any existing or stale notificationretention jobs are drained without running retention.
- **Comment**: Top-of-file comment updated to state that notificationretention is deprecated (Phase 7) and drained only; real retention is via INotificationRetentionService + NotificationRetentionHostedService.

---

## E. Retention safety/scope defined

- **Eligible for archive**: Rows in `Notifications` where `Status` is Read or Unread, and `CreatedAt < (UtcNow - archiveDays)`. No company filter when `companyId` is null; when `companyId` is set, only rows with that `CompanyId` are considered.
- **Eligible for delete**: Rows where `Status == "Archived"` and `ArchivedAt` is set and `ArchivedAt < (UtcNow - deleteDays)`. Same company filter as above.
- **Operation**: Archive is in-place update (Status, ArchivedAt, UpdatedAt). Delete is hard delete (`RemoveRange` + `SaveChanges`). No soft delete; no auxiliary tables (e.g. NotificationDispatches) are purged by this service.
- **Risks**: (1) If `companyId` is null, all companies’ data is processed (same as legacy). (2) Hard delete is irreversible; operational visibility is via logs and optional future audit table.

---

## F. Operational visibility

- **Logging**: Each run logs archived count, deleted count, and (when company-scoped) companyId. Hosted service logs “Notification retention run: archived=X, deleted=Y” after each run.
- **Configuration**: `Notifications:Retention` (IntervalHours, ArchiveDays, DeleteDays) for the hosted service. No new API or reporting layer in this phase; visibility is via logs and config.

---

## G. Tests added

- **NotificationRetentionServiceTests**:
  - `RunRetentionAsync_ArchivesOldReadUnread_AndDeletesOldArchived`: Ensures old Read/Unread are archived and old Archived are deleted; counts and final state asserted.
  - `RunRetentionAsync_WhenCompanyIdSet_ScopesToCompany`: Only the specified company’s notifications are archived.
  - `RunRetentionAsync_DoesNotArchiveRecent`: Notifications newer than archive window are not archived.
  - `RunRetentionAsync_DoesNotDeleteRecentlyArchived`: Recently archived notifications are not deleted.
  - `RunRetentionAsync_InvalidDays_UseDefaults`: Zero/negative days fall back to default behavior (archive 90, delete 365).

---

## H. Migrations added

- **None.** Only the `Notifications` table is used; no schema changes.

---

## I. Remaining legacy job debt

- Legacy BackgroundJob still owns: **emailingest**, **myinvoisstatuspoll**, **inventoryreportexport**, **eventhandlingasync**, **operationalreplay**, **operationalrebuild**.
- **notificationretention** remains as a job type in JobDefinitionProvider for observability; execution is deprecated (drain only).

---

## J. Recommended Phase 8 extraction candidate

- **emailingest** or **myinvoisstatuspoll**: Next high-value extractions; choose based on product priority (email ingestion is broader; MyInvois is billing-specific).

---

## K. Files/docs created or updated

### Created
- `backend/src/CephasOps.Application/Notifications/INotificationRetentionService.cs`
- `backend/src/CephasOps.Application/Notifications/Services/NotificationRetentionService.cs`
- `backend/src/CephasOps.Application/Notifications/NotificationRetentionHostedService.cs` (includes NotificationRetentionOptions)
- `backend/tests/CephasOps.Application.Tests/Notifications/NotificationRetentionServiceTests.cs`
- `docs/PHASE7_NOTIFICATION_RETENTION_EXTRACTION.md`

### Updated
- `backend/src/CephasOps.Application/Workflow/Services/BackgroundJobProcessorService.cs`: notificationretention routed to `ProcessNotificationRetentionJobDeprecatedAsync` (no-op, log, return true); removed `ProcessNotificationRetentionJobAsync`; updated legacy comment.
- `backend/src/CephasOps.Api/Program.cs`: registered INotificationRetentionService, NotificationRetentionOptions, NotificationRetentionHostedService.
