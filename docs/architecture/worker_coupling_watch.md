# Worker Coupling Watch

**Related:** [background_worker_map.md](background_worker_map.md) | [worker_dependency_risks.md](worker_dependency_risks.md) | [ARCHITECTURE_WATCHDOG_REPORT.md](../ARCHITECTURE_WATCHDOG_REPORT.md)

**Purpose:** Monitor workers and hosted services for increasing complexity—workers calling more services, becoming orchestrators of many domains, or introducing hidden dependencies. Governance-level only.

**Last scan:** March 2026 (Level 15 watchdog). **Hosted service count:** 15 (matches background_worker_map and Program.cs).

---

## Worker coupling register

| Worker | Coupling level | Affected domains | Risk trend | Related docs |
|--------|----------------|------------------|------------|---------------|
| **BackgroundJobProcessorService** | **High** | Parser, P&L, Inventory, Billing, Events, Replay, Rebuild, SLA | Stable (no new job types in this scan) | background_jobs, worker_dependency_risks |
| **EventStoreDispatcherHostedService** | **High** | All domains (via handlers) | Stable | EVENT_BUS_OPERATIONS_RUNBOOK, PHASE_8_PLATFORM_EVENT_BUS |
| **NotificationDispatchWorkerHostedService** | Medium | Notifications, Orders (context) | Stable | 02_modules/notifications |
| **JobExecutionWorkerHostedService** | Medium | P&L, Replay (IJobExecutorRegistry) | Stable | background_jobs |
| **EmailIngestionSchedulerService** | Medium | Parser (enqueues EmailIngest) | Stable | 02_modules/email_parser |
| **PnlRebuildSchedulerService** | Medium | P&L (enqueues PnlRebuild) | Stable | 02_modules/pnl |
| **SlaEvaluationSchedulerService** | Medium | SLA (enqueues slaevaluation) | Stable | background_jobs |
| **PayoutAnomalyAlertSchedulerService** | Medium | Rates, Orders (payout snapshots) | Stable | background_jobs |
| **MissingPayoutSnapshotSchedulerService** | Medium | Rates, Orders | Stable | background_jobs |
| **StockSnapshotSchedulerService** | Low–Medium | Inventory (enqueues snapshot job) | Stable | background_jobs |
| **LedgerReconciliationSchedulerService** | Low–Medium | Inventory (enqueues reconcile job) | Stable | background_jobs |
| **EventBusMetricsCollectorHostedService** | Low | Events (read-only metrics) | Stable | EVENT_BUS_OPERATIONS_RUNBOOK |
| **WorkerHeartbeatHostedService** | Low | Infrastructure (liveness) | Stable | background_jobs |
| **JobPollingCoordinatorService** | Low | Job orchestration | Stable | background_jobs |
| **EmailCleanupService** | Low | Parser (mail viewer TTL) | Stable | background_jobs |

---

## Watch signals (reference)

- **Workers calling more services over time:** e.g. BackgroundJobProcessorService adding new job types that resolve new domain services.
- **Workers becoming orchestrators:** single worker dispatching to many business domains (already true for BackgroundJobProcessorService and EventStoreDispatcherHostedService).
- **Hidden dependencies in worker pipeline:** new GetRequiredService in a worker or in a job handler.
- **Metrics/heartbeat/scheduler reaching into business domains:** e.g. WorkerHeartbeatHostedService or JobPollingCoordinatorService starting to call OrderService or BillingService (currently they do not).

Current state: No new hosted services or job types detected since Level 14. Coupling levels unchanged. Re-scan when AddHostedService or new job type handlers are added.

---

**Refresh:** When adding or changing hosted services, or when a worker gains new GetRequiredService or new job types, update this table and worker_dependency_risks.md; update ARCHITECTURE_WATCHDOG_REPORT § Worker coupling.
