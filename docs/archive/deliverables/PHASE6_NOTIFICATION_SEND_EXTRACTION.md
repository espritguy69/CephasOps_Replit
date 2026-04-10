# Phase 6: Notification Send Extraction

## A. Legacy notificationsend flow audited

### Where notificationsend was defined and processed
- **Processor**: `BackgroundJobProcessorService.ProcessNotificationSendJobAsync` (removed in Phase 6).
- **Payload**: `{ "notificationId": "<guid>" }` (required).
- **Behavior**: Loaded `Notification` by id, read `DeliveryChannels` (comma-separated: InApp, Email, SMS, WhatsApp). For **Email**: resolved user email from `Users`, first active `EmailAccount`, called `IEmailSendingService.SendEmailAsync` (subject = notification.Title, body = notification.Message). SMS/WhatsApp were not implemented (log only). On success, updated `Notification.MetadataJson` with `emailSentAt` and `emailSent`. Returned true if email sent or if InApp was in channels.

### Producers (who created notificationsend jobs)
- **None found in codebase.** No call site creates a `BackgroundJob` with `JobType = "notificationsend"`. `NotificationService.CreateNotificationAsync` only persists the `Notification` entity; it did not enqueue a BackgroundJob. The only producer that set `DeliveryChannels` to include Email was **EmailIngestionService** (VIP notification: `"InApp,Email"`), but it never enqueued a notificationsend job. So the legacy path was **dead from a producer standpoint**: the processor could run if a job existed (e.g. manual or old client), but no current code created such jobs.

### Overlap with NotificationDispatch
- **Phase 2 NotificationDispatch**: Order-status–driven; `INotificationDispatchRequestService.RequestOrderStatusNotificationAsync` creates `NotificationDispatch` rows (Sms/WhatsApp, template-based). `NotificationDispatchWorkerHostedService` claims pending rows and sends via `INotificationDeliverySender` (Sms, WhatsApp only; Email was not supported before Phase 6).
- **Legacy notificationsend**: In-app `Notification` entity + Email (and placeholder SMS/WhatsApp). No template; subject/body from notification.
- **Conclusion**: No duplicate-send risk today (no producers). Overlap is conceptual: “send a notification to the user” — legacy by job + processor, modern by dispatch row + worker. Convergence: use the same dispatch pipeline for “send notification email” by adding Email support to the dispatch path and requesting a dispatch when creating a notification with Email channel.

### Retry and audit
- Legacy: BackgroundJob retries (MaxRetries = 3), JobRun recording, display name “Notification Send” in `JobDefinitionProvider`. No idempotency key; duplicate jobs would send duplicate emails.

---

## B. Convergence model chosen

- **Target: NotificationDispatch pipeline** (not JobExecution).
- **Reason**: Sending a notification email is “deliver this message to this address”; that matches the existing NotificationDispatch model (channel, target, payload). JobExecution is used for heavier workflows (PnL, ledger, document generation); adding “send one email” there would duplicate responsibility. So we extended the existing dispatch pipeline with **Email** channel and moved the only logical producer (notifications created with `DeliveryChannels` containing Email) onto it.
- **Implementation**: (1) When `NotificationService.CreateNotificationAsync` is called with `DeliveryChannels` containing `"Email"`, after saving the notification it requests an email dispatch: resolves user email, checks idempotency key `{notificationId}:Email`, adds a `NotificationDispatch` row (Channel = Email, Target = user email, PayloadJson = subject/body). (2) `INotificationDeliverySender` (NotificationDeliverySender) extended to handle Channel `"Email"` using `IEmailSendingService` and `IDefaultEmailAccountIdProvider`. (3) Legacy `notificationsend` handler removed from `BackgroundJobProcessorService` and replaced with a deprecation no-op that marks the job complete without sending (drains stale jobs).

---

## C. Producer paths migrated

- **Single producer path**: Notifications created with `DeliveryChannels` including `"Email"` (e.g. VIP email in `EmailIngestionService`: `CreateNotificationAsync` with `DeliveryChannels = "InApp,Email"`). That path now triggers **email via NotificationDispatch** instead of relying on a (nonexistent) notificationsend job.
- **Flow**: `CreateNotificationAsync` → save notification → if channels contain Email → `RequestEmailDispatchForNotificationAsync` (resolve user email, idempotency check, `_dispatchStore.AddAsync(dispatch)`). Worker later sends via `NotificationDeliverySender.SendAsync` (Email branch).

---

## D. Legacy BackgroundJob responsibility reduced

- **notificationsend** is no longer executed for send logic. `BackgroundJobProcessorService` now routes `"notificationsend"` to `ProcessNotificationSendJobDeprecatedAsync`, which logs a deprecation warning and returns `true` so the job is marked completed (no retry storm). Any existing or stale notificationsend jobs in the queue are drained without sending.
- **Comment and display name**: Top-of-file comment updated to state that notificationsend is deprecated (Phase 6) and drained only. `JobDefinitionProvider` still defines `notificationsend` for observability (display name “Notification Send”).

---

## E. Idempotency / retry behavior

- **Idempotency**: For each notification that requests email, we use idempotency key `{notificationId}:Email`. Before adding a dispatch we call `_dispatchStore.ExistsByIdempotencyKeyAsync(idempotencyKey)`. If it exists we skip `AddAsync`, so duplicate requests for the same notification do not create a second email dispatch.
- **Retry**: NotificationDispatch rows use existing worker retry (AttemptCount, MaxAttempts = 5, NextRetryAtUtc, MarkProcessedAsync with backoff). No change to that behavior.
- **Duplicate-send risk**: If the same notification id were used to request email twice before the first dispatch was written, two dispatches could be created (the idempotency check is “already a row with this key”). In practice the only caller is `CreateNotificationAsync` immediately after creating one notification, so one notification id ⇒ one request. Risk is documented; mitigation is the single-write path and idempotency key.

---

## F. Operational visibility

- Migrated email sends appear as **NotificationDispatch** rows (Channel = Email, Status Pending/Processing/Sent/Failed/DeadLetter). Existing notification and job/execution query paths are unchanged. No new API endpoints; dispatches are visible via existing store/query mechanisms. `JobDefinitionProvider` still exposes `notificationsend` for any legacy job runs in the UI.

---

## G. Tests added

- **NotificationServiceTests** (Phase 6):
  - `CreateNotificationAsync_WithInAppOnly_DoesNotCallDispatchStore`: DeliveryChannels = InApp ⇒ AddAsync not called.
  - `CreateNotificationAsync_WithEmailChannel_EnqueuesEmailDispatch_AndPreservesCompanyId`: User with email, DeliveryChannels = InApp,Email ⇒ one dispatch added with Channel Email, correct Target, CompanyId, IdempotencyKey, PayloadJson with subject/body.
  - `CreateNotificationAsync_WithEmailChannel_UserHasNoEmail_DoesNotEnqueue`: User with empty email ⇒ AddAsync not called.
  - `CreateNotificationAsync_WithEmailChannel_IdempotencyKeyAlreadyExists_DoesNotCallAddAsync`: ExistsByIdempotencyKeyAsync returns true ⇒ AddAsync not called.
- **NotificationService** constructor** now takes `INotificationDispatchStore`; existing tests updated with a mock store.

---

## H. Migrations

- **No database migrations** in this phase. `NotificationDispatches` table and schema already support Channel and PayloadJson; we use Channel = `"Email"` and PayloadJson with `subject`/`body`. No schema change required.

---

## I. Remaining legacy job debt

- Legacy BackgroundJob still owns: **emailingest**, **notificationretention**, **myinvoisstatuspoll**, **inventoryreportexport**, **eventhandlingasync**, **operationalreplay**, **operationalrebuild**.
- **notificationsend** remains as a job type only for definition/observability; execution is deprecated (drain-only).

---

## J. Recommended Phase 7 extraction candidate

- **notificationretention**: Next logical step after notificationsend. Audit where retention jobs are created, then move retention work to a dedicated service or JobExecution and remove from BackgroundJobProcessorService.

---

## K. Files created or updated

### Created
- `backend/src/CephasOps.Application/Notifications/IDefaultEmailAccountIdProvider.cs`
- `backend/src/CephasOps.Application/Notifications/Services/DefaultEmailAccountIdProvider.cs`
- `docs/PHASE6_NOTIFICATION_SEND_EXTRACTION.md`

### Updated
- `backend/src/CephasOps.Application/Notifications/Services/NotificationService.cs` — inject `INotificationDispatchStore`, request email dispatch when DeliveryChannels contains Email; added `RequestEmailDispatchForNotificationAsync`.
- `backend/src/CephasOps.Application/Notifications/Services/NotificationDeliverySender.cs` — optional `IEmailSendingService` and `IDefaultEmailAccountIdProvider`; handle Channel `"Email"` in `SendAsync`; new `SendEmailAsync`.
- `backend/src/CephasOps.Application/Workflow/Services/BackgroundJobProcessorService.cs` — notificationsend routed to `ProcessNotificationSendJobDeprecatedAsync` (no-op, log, return true); removed `ProcessNotificationSendJobAsync`; updated legacy comment.
- `backend/src/CephasOps.Api/Program.cs` — register `IDefaultEmailAccountIdProvider` → `DefaultEmailAccountIdProvider`.
- `backend/tests/CephasOps.Application.Tests/Notifications/NotificationServiceTests.cs` — inject mock `INotificationDispatchStore`; added CreateNotificationAsync tests for email dispatch and idempotency.
