# Worker Dependency Risks

**Related:** [background_worker_map.md](background_worker_map.md) | [hidden_dependencies.md](hidden_dependencies.md) | [REFACTOR_SAFETY_REPORT.md](../REFACTOR_SAFETY_REPORT.md)

**Purpose:** Audit of background workers and schedulers: which services they call, which modules they affect, and where they create hidden or high-risk coupling.

---

## Workers and their service dependencies

| Worker | Services / dependencies resolved or injected | Modules affected | Risk | Notes |
|--------|---------------------------------------------|------------------|------|------|
| **BackgroundJobProcessorService** | ApplicationDbContext, IWorkerCoordinator; per job type: IOperationalRebuildService, IEventStore, IEventTypeRegistry, IJobRunRecorderForEvents, IOperationalReplayExecutionService, ISlaEvaluationService, IEmailIngestionService, IPnlService, IStockLedgerService, ICsvService, EInvoiceProviderFactory, IInvoiceSubmissionService | Parser, P&L, Inventory, Billing, Events, Replay, Rebuild, SLA | **High** | Single processor; many GetRequiredService calls. Changing any job handler or service signature can break that job type. No single “module”; cross-domain by design. |
| **EmailIngestionSchedulerService** | ApplicationDbContext; enqueues EmailIngest (processed by BackgroundJobProcessorService → IEmailIngestionService) | Parser | Medium | Indirect: job processor invokes Parser. Scheduler itself is thin. |
| **StockSnapshotSchedulerService** | ApplicationDbContext, IJobExecutionEnqueuer | Inventory (via job) | Low–Medium | Enqueues job; actual work in job executor (IStockLedgerService). |
| **LedgerReconciliationSchedulerService** | ApplicationDbContext, IJobExecutionEnqueuer | Inventory (via job) | Low–Medium | Same pattern; ReconcileLedgerBalanceCache job. |
| **PnlRebuildSchedulerService** | ApplicationDbContext, IJobExecutionEnqueuer | P&L (via JobExecutionWorker) | Medium | PnlRebuild runs in JobExecutionWorker; depends on Pnl and Orders data. |
| **SlaEvaluationSchedulerService** | ApplicationDbContext, IJobExecutionEnqueuer | SLA (slaevaluation job) | Medium | slaevaluation job in BackgroundJobProcessorService → ISlaEvaluationService. |
| **PayoutAnomalyAlertSchedulerService** | ApplicationDbContext, IPayoutAnomalyAlertService | Rates, Orders (payout snapshots) | Medium | Runs alerts in-process; touches payout and order data. |
| **MissingPayoutSnapshotSchedulerService** | ApplicationDbContext, IMissingPayoutSnapshotRepairService | Rates, Orders | Medium | Backfill snapshots; repair service. |
| **EmailCleanupService** | ApplicationDbContext, IFileService (GetRequiredService) | Parser (mail viewer storage), Files | Low | 48h TTL cleanup; not on critical path. |
| **NotificationDispatchWorkerHostedService** | INotificationDeliverySender, template services | Notifications, Orders (context in dispatch requests) | Medium | Sends outbound; dispatch request may carry order/SI context. |
| **EventStoreDispatcherHostedService** | IEventStore, IEventTypeRegistry, IDomainEventDispatcher (GetRequiredService per cycle) | All domains (handlers) | **High** | Dispatcher invokes all registered handlers; handler changes affect every event type they handle. |
| **EventBusMetricsCollectorHostedService** | IEventStoreQueryService (GetRequiredService) | Events (read-only metrics) | Low | Observability only. |
| **JobExecutionWorkerHostedService** | IJobExecutorRegistry (PnlRebuild, OperationalReplay, etc.) | P&L, Replay, orchestrated jobs | Medium | Executors are registered per job type; adding/removing executors changes module impact. |
| **WorkerHeartbeatHostedService** | IWorkerCoordinator (GetRequiredService) | Workers (infrastructure) | Low | Liveness only. |
| **JobPollingCoordinatorService** | ApplicationDbContext, IWorkerCoordinator, IWorkerIdentity, WorkerOptions | Job orchestration | Low | Coordination; no domain logic. |

---

## Hidden coupling introduced by workers

- **BackgroundJobProcessorService:** One type name (e.g. "MyInvoisStatusPoll") binds to a code path that resolves IInvoiceSubmissionService at runtime. New job types or moved services can break without compile-time signal. **Recommendation:** Document every job type and its resolved service in [background_jobs.md](../operations/background_jobs.md) or [background_worker_map.md](background_worker_map.md); consider job-type → handler registry doc.
- **EventStoreDispatcherHostedService:** Handler set is implicit (registration in DI). Adding a handler that touches Orders, Billing, or Inventory creates coupling not visible in module dependency maps. **Recommendation:** Maintain a list of event types and handler responsibilities (e.g. in EVENT_BUS_OPERATIONS_RUNBOOK or a dedicated handler index).
- **OrderStatusChangedNotificationHandler:** Resolves IOrderService and IGlobalSettingsService at runtime. Event handler in Notifications domain depends on Orders and Settings; not visible on any controller. **Recommendation:** Document in [hidden_dependencies.md](hidden_dependencies.md) (done); consider constructor injection for testability.

---

## Workers affecting multiple modules (summary)

| Worker | Module count (effective) | Notes |
|--------|---------------------------|-------|
| BackgroundJobProcessorService | 8+ | Parser, P&L, Inventory, Billing, Events, Replay, Rebuild, SLA. |
| EventStoreDispatcherHostedService | All | Handlers per event type. |
| NotificationDispatchWorkerHostedService | 2 | Notifications, Orders (context). |
| JobExecutionWorkerHostedService | 2+ | P&L, Replay, and any future orchestrated job. |
| PayoutAnomalyAlertSchedulerService, MissingPayoutSnapshotSchedulerService | 2 | Rates, Orders. |

---

**Refresh:** When adding or changing a hosted service or job type, update this file and [background_worker_map.md](background_worker_map.md); add any new GetRequiredService to [hidden_dependencies.md](hidden_dependencies.md).
