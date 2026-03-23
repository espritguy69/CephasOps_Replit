# Known Bypasses and Guards

**Date:** 2026-03-13  
**Purpose:** Document intentional platform bypasses and application guards so that tenant safety is auditable and no undocumented bypass weakens isolation.

---

## 1. Intentional Platform Bypasses

Platform bypass (**TenantSafetyGuard.EnterPlatformBypass** or **TenantScopeExecutor.RunWithPlatformBypassAsync**) turns off SaveChanges tenant validation and allows AssertTenantContext to pass without a tenant. **Only** the following are allowed:

| Location | Purpose | Pairing |
|----------|---------|---------|
| **DatabaseSeeder** | One-time bootstrap/setup seeding | Enter before seeding, Exit in finally. Process-bound. |
| **ApplicationDbContextFactory.CreateDbContext** | Design-time EF Core (migrations, tooling) | Enter in CreateDbContext; no Exit (process exits). |
| **EventPlatformRetentionService / NotificationRetentionService** (when companyId null) | Retention/cleanup across tenants | TenantScopeExecutor.RunWithPlatformBypassAsync |
| **Scheduler enumeration loops** (EmailIngestionSchedulerService, PnlRebuildSchedulerService, LedgerReconciliationSchedulerService, StockSnapshotSchedulerService, MissingPayoutSnapshotSchedulerService, PayoutAnomalyAlertSchedulerService) | Enumerate tenants to enqueue per-tenant work | TenantScopeExecutor.RunWithPlatformBypassAsync for the loop; per-tenant work uses tenant scope or jobs with CompanyId |
| **SlaEvaluationSchedulerService** | Per-company SLA evaluation | TenantScopeExecutor.RunWithTenantScopeAsync(companyId, …) per company |
| **CompanyProvisioningService** | Create Tenant, Company, departments, admin | TenantScopeExecutor.RunWithPlatformBypassAsync (generic overload returns result) |
| **InboundWebhookRuntime** (when request.CompanyId null/empty) | Webhook with no company | TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(…) → bypass branch |
| **EventStoreDispatcherHostedService / EventReplayService** (when entry.CompanyId null) | Event with no company | TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(…) → bypass branch |
| **BackgroundJobProcessorService** (reap path) | Reset stuck running jobs | TenantScopeExecutor.RunWithPlatformBypassAsync |

**Rule:** No new bypass for "convenience." Any new bypass must be justified, documented here or in the architecture doc, and must pair ExitPlatformBypass (or executor) so scope is restored.

---

## 2. Runtime Standard: TenantScopeExecutor

**Manual** EnterPlatformBypass/ExitPlatformBypass and manual TenantScope set/restore are **not** used in normal runtime services. All hosted services, schedulers, event dispatchers, replay, webhooks, and job workers use **TenantScopeExecutor** (RunWithTenantScopeAsync, RunWithPlatformBypassAsync, or RunWithTenantScopeOrBypassAsync). See backend [TENANT_SCOPE_EXECUTOR_COMPLETION.md](../../backend/docs/architecture/TENANT_SCOPE_EXECUTOR_COMPLETION.md).

---

## 3. Application Guards

| Guard | Purpose |
|-------|---------|
| **TenantSafetyGuard** (SaveChanges) | Final fail-closed: require tenant context (or platform bypass) when saving tenant-scoped entities; enforce entity CompanyId == CurrentTenantId for Modified/Deleted and for Added when CompanyId is set. |
| **TenantGuardMiddleware** | Block tenant-required routes when effective company cannot be resolved; return 403. |
| **SiWorkflowGuard** | Workflow and SI-specific business rules (no tenant bypass). |
| **FinancialIsolationGuard** | Financial correctness and isolation (no cross-tenant financial leakage). |
| **EventStoreConsistencyGuard** | Event store consistency (no cross-tenant event corruption). |

These must not be weakened (e.g. no skipping tenant check for "convenience").

---

## 4. Null-Company / Fail-Closed

- **Tenant-owned work** (enqueue job, create notification, create dispatch): when company cannot be resolved (null/empty), **do not** create tenant-scoped entities; skip or early-return with log, or throw/403. Do not treat null/empty as "all tenants."
- **API:** When tenant is required and unresolved, TenantGuardMiddleware returns 403.

---

*See: backend [TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md), [SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md](../../backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md), [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md).*
