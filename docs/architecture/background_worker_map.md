# Background Worker Map

**Related:** [operations/background_jobs.md](../operations/background_jobs.md) | [CODEBASE_INTELLIGENCE_MAP.md](CODEBASE_INTELLIGENCE_MAP.md)

**Purpose:** Hosted services and schedulers: purpose, trigger/schedule, key dependencies, related controllers/docs.

---

## Hosted services (in-process)

| Worker | Purpose | Trigger / schedule | Key dependencies | Related controllers / admin | Docs |
|--------|---------|--------------------|------------------|-----------------------------|------|
| **BackgroundJobProcessorService** | Process queued jobs from BackgroundJob table | Poll table; run job handlers | Job type handlers (EmailIngest, PnlRebuild, etc.) | BackgroundJobsController | background_jobs.md |
| **EmailIngestionSchedulerService** | Enqueue EmailIngest jobs | Schedule (per account) | IEmailIngestionService (via job) | ParserController | background_jobs.md, 02_modules/email_parser |
| **StockSnapshotSchedulerService** | Enqueue PopulateStockByLocationSnapshots | Daily; 6h check | Stock snapshot job handler | — | background_jobs.md |
| **LedgerReconciliationSchedulerService** | Enqueue ReconcileLedgerBalanceCache | 12h check | Ledger service | — | background_jobs.md |
| **PnlRebuildSchedulerService** | Enqueue PnlRebuild | Daily; current month | Pnl rebuild job handler | PnlController | background_jobs.md, 02_modules/pnl |
| **SlaEvaluationSchedulerService** | Enqueue slaevaluation | Every 15 min | Sla evaluation handler | — | background_jobs.md |
| **PayoutAnomalyAlertSchedulerService** | Run payout anomaly alerts | Configurable interval (PayoutAnomalyAlert:SchedulerEnabled) | IPayoutAnomalyAlertService | PayoutHealthController | background_jobs.md |
| **MissingPayoutSnapshotSchedulerService** | Enqueue/trigger missing payout snapshot backfill | When configured | Payout snapshot job | PayoutHealthController | background_jobs.md |
| **EmailCleanupService** | 48h TTL cleanup for mail viewer | Scheduled | Parser email storage | — | background_jobs.md |
| **NotificationDispatchWorkerHostedService** | Claim NotificationDispatch; send via SMS/WhatsApp/email | Poll pending rows; Options: Notifications:DispatchWorker | INotificationDeliverySender, template services | NotificationsController | background_jobs.md, 02_modules/notifications |
| **EventStoreDispatcherHostedService** | Poll event store; dispatch to handlers; mark dispatched | Continuous poll | IEventStore, DomainEventDispatcher | EventStoreController, EventsController | EVENT_BUS_OPERATIONS_RUNBOOK, PHASE_8_PLATFORM_EVENT_BUS |
| **EventBusMetricsCollectorHostedService** | Event bus throughput/lag metrics | Periodic | Event store / dispatcher metrics | — | EVENT_BUS_OPERATIONS_RUNBOOK |
| **JobExecutionWorkerHostedService** | Claim JobExecution rows; run via IJobExecutorRegistry | Poll; Options: JobOrchestration:Worker | IJobExecutorRegistry (e.g. PnlRebuild, OperationalReplay) | JobOrchestrationController | background_jobs.md |
| **WorkerHeartbeatHostedService** | Worker node heartbeat / liveness | Periodic | Worker identity store | SystemWorkersController | background_jobs.md |
| **JobPollingCoordinatorService** | Coordinate job polling (orchestration) | Optional; coordinates with JobExecutionWorker | Job orchestration | JobOrchestrationController | background_jobs.md |

---

## Job types (consumed by BackgroundJobProcessorService)

| Job type | Purpose | Enqueued by |
|----------|---------|-------------|
| EmailIngest | Fetch and parse email; create sessions/drafts | EmailIngestionSchedulerService |
| PnlRebuild | Rebuild P&L aggregation | PnlRebuildSchedulerService or JobOrchestration |
| NotificationSend | Send notification | Notification flow |
| NotificationRetention | Cleanup old notifications | Scheduler |
| DocumentGeneration | Heavy document generation | On-demand |
| MyInvoisStatusPoll | Poll MyInvois after submission | Billing flow |
| InventoryReportExport | Large inventory export | Reports |
| ReconcileLedgerBalanceCache | Reconcile ledger cache | LedgerReconciliationSchedulerService |
| PopulateStockByLocationSnapshots | Stock-by-location snapshots | StockSnapshotSchedulerService |
| slaevaluation | SLA evaluation; record breaches | SlaEvaluationSchedulerService |

---

**Refresh:** When adding or removing HostedServices or job types, update this map and operations/background_jobs.md.
