# Background Jobs & Schedulers

**Related:** [Integrations overview](../integrations/overview.md) | [Product overview](../overview/product_overview.md) | [02_modules/background_jobs/OVERVIEW](../02_modules/background_jobs/OVERVIEW.md) | [08_infrastructure/background_jobs_infrastructure](../08_infrastructure/background_jobs_infrastructure.md)

**Source of truth:** Codebase Summary (Senior Architect Review); Business Processes (Business Systems Analyst Report).

---

## 1. Mechanism

- **In-process:** No Hangfire or Quartz. A **BackgroundJob** table stores jobs; a **BackgroundJobProcessorService** (HostedService) polls and executes them.
- **Job states:** Queued, Running, Succeeded, Failed. Retries with backoff when configured.

---

## 2. Hosted services (schedulers)

| Service | Purpose |
|---------|---------|
| **BackgroundJobProcessorService** | Processes queued jobs from the table. |
| **EmailIngestionSchedulerService** | Enqueues **EmailIngest** jobs (poll email accounts). |
| **StockSnapshotSchedulerService** | Enqueues **PopulateStockByLocationSnapshots** (daily; every 6h check). |
| **LedgerReconciliationSchedulerService** | Enqueues **ReconcileLedgerBalanceCache** (every 12h check; no duplicate pending). |
| **PnlRebuildSchedulerService** | Enqueues **PnlRebuild** (daily; current month; first active company). |
| **SlaEvaluationSchedulerService** | Enqueues **slaevaluation** (every 15 min; one job when none pending). |
| **PayoutAnomalyAlertSchedulerService** | Runs payout anomaly alerting in-process on a configurable interval (when `PayoutAnomalyAlert:SchedulerEnabled` is true). Does not enqueue a job; calls `IPayoutAnomalyAlertService.RunAlertsAsync` directly. |
| **MissingPayoutSnapshotSchedulerService** | Enqueues or triggers missing payout snapshot backfill when configured. |
| **EmailCleanupService** | 48-hour TTL cleanup for mail viewer data. |
| **NotificationDispatchWorkerHostedService** | Claims pending NotificationDispatch rows; sends via INotificationDeliverySender (SMS/WhatsApp/email); marks Sent/Failed/DeadLetter. Options: Notifications:DispatchWorker. |
| **EventStoreDispatcherHostedService** | Polls event store for undispatched events; dispatches to domain handlers; marks dispatched. Core of outbox/event-bus processing. |
| **EventBusMetricsCollectorHostedService** | Collects event bus metrics (throughput, lag) for observability. |
| **JobExecutionWorkerHostedService** | Claims jobs from JobExecution store; runs via IJobExecutorRegistry (e.g. PnlRebuild, OperationalReplay); marks succeeded/failed. Options: JobOrchestration:Worker. |
| **WorkerHeartbeatHostedService** | Periodic heartbeat for worker nodes (worker identity and liveness). |
| **JobPollingCoordinatorService** | Coordinates polling for job orchestration (optional; may coordinate with JobExecutionWorker). |

---

## 3. Job types (processed by processor)

| Job type | Purpose |
|----------|---------|
| EmailIngest | Fetch and parse email; create parse sessions and drafts. |
| PnlRebuild | Rebuild P&L aggregation. |
| NotificationSend | Send notifications. |
| NotificationRetention | Cleanup/retention of old notifications. |
| DocumentGeneration | Heavy document generation. |
| MyInvoisStatusPoll | Poll MyInvois for e-invoice status after submission. |
| InventoryReportExport | Large inventory report export (async). |
| ReconcileLedgerBalanceCache | Reconcile cached ledger balances with actual ledger. |
| PopulateStockByLocationSnapshots | Daily stock-by-location snapshots for reporting. |
| slaevaluation | SLA evaluation: evaluates SlaRules, records breaches (WorkflowTransition, EventProcessing, BackgroundJob, EventChainStall). |

---

## 4. UI

- **Admin → Background Jobs** page; **BackgroundJobsController** (recent jobs, summary, counts by state).
