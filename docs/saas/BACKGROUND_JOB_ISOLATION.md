# Background Job Isolation

**Date:** 2026-03-13  
**Purpose:** Tenant-aware job scheduling, per-tenant job fairness, tenant-scoped job execution, logging of CompanyId, and failure isolation. Ensures jobs cannot leak tenant data.

---

## 1. Tenant-Aware Job Scheduling

- **BackgroundJob** and **JobExecution** carry **CompanyId** (nullable). Jobs are enqueued with the tenant's CompanyId for tenant-owned work; platform-wide jobs may have null CompanyId.
- **BackgroundJobProcessorService** (and worker) **claims** jobs (e.g. from a single queue or table) and runs each job under the correct scope. Claim logic may use **IgnoreQueryFilters** to read pending jobs from all tenants, but **execution** is always under **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(job.CompanyId, ...)** so that business logic runs in tenant scope or explicit platform bypass.

---

## 2. Per-Tenant Job Fairness

- To prevent one tenant from starving others, the worker can limit the number of jobs **claimed per tenant per poll cycle** (e.g. **MaxJobsPerTenantPerCycle**). Claim logic ranks by (CompanyId, Priority, CreatedAtUtc) and caps jobs per CompanyId before claiming. See docs/saas_scaling/JOB_ISOLATION.md for configuration (JobOrchestration:Worker:MaxJobsPerTenantPerCycle, TenantJobFairnessEnabled).

---

## 3. Tenant-Scoped Job Execution

- **No manual TenantScope or EnterPlatformBypass** in the job worker. The worker uses **TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(job.CompanyId, ...)** so that:
  - If job.CompanyId has a value → run under tenant scope (TenantScope.CurrentTenantId = job.CompanyId).
  - If job.CompanyId is null/empty → run under platform bypass (only for intended platform-wide jobs).
- All tenant-scoped persistence inside the job delegate then sees the correct tenant context and EF filters and SaveChanges behave correctly.

---

## 4. Logging of CompanyId for Job Runs

- Job execution logs should include **CompanyId** (and optionally JobType, ExecutionTimeMs, JobId) so that operations can attribute runs to tenants and debug tenant-specific issues. RequestLogContextMiddleware pattern does not apply to jobs; use explicit log properties in the job runner.

---

## 5. Failure Isolation

- A failing job in one tenant must **not** affect other tenants: no shared in-memory state; each job runs in its own scope. Subscription/tenant access checks (e.g. before execution) can mark jobs as failed (non-retryable) for denied tenants without executing them. Stuck job recovery (e.g. reset by lease expiry) is per job; re-claim is again subject to tenant fairness.

---

## 6. References

- **Backend:** BackgroundJobProcessorService; JobExecutionWorkerHostedService; TenantScopeExecutor; JobOrchestration:Worker config.
- **Docs:** [docs/saas_scaling/JOB_ISOLATION.md](../saas_scaling/JOB_ISOLATION.md), [TENANCY_MODEL.md](TENANCY_MODEL.md), backend [TENANT_SAFETY_DEVELOPER_GUIDE.md](../../backend/docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md).

---

*See also: [DATA_ISOLATION_RULES.md](DATA_ISOLATION_RULES.md), [KNOWN_BYPASSES_AND_GUARDS.md](KNOWN_BYPASSES_AND_GUARDS.md).*
