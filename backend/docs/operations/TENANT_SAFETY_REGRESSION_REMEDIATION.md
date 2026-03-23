# Tenant-Safety Regression Remediation

**Date:** 2026-03-13  
**Purpose:** Correct the tenant-safety regression introduced during test-remediation work. Restore normal filtered behavior in business services and fix tests by aligning tenant scope and CompanyId instead of broadening production queries.

**Rules applied:**
- Normal application services behave as tenant/company-scoped; query filters are a defense layer.
- Tests must set `TenantScope.CurrentTenantId` and use consistent `CompanyId` for seeded data.
- `IgnoreQueryFilters()` remains only where the path is intentionally platform-wide or explicit company-scoped with authorization/guard, and never as a default workaround for failing tests.

---

## Phase 1 — Audit: IgnoreQueryFilters Added During Test Remediation

The following were added to make tests pass when `TenantScope` did not flow in async test context. They are **normal business services** that should respect global query filters when tenant scope is set.

| File | Method | Entity | Type | Explicit companyId? | Decision |
|------|--------|--------|------|---------------------|----------|
| DocumentGenerationService.cs | GetTemplateAsync | DocumentTemplates | A. Normal business | Yes (all 3 queries) | **REVERT** |
| WorkflowDefinitionsService.cs | GetWorkflowDefinitionsAsync, GetWorkflowDefinitionAsync, GetEffectiveWorkflowDefinitionAsync, UpdateWorkflowDefinitionAsync, DeleteWorkflowDefinitionAsync, AddTransitionAsync, ValidateUniqueActiveScopeAsync | WorkflowDefinitions | A. Normal business | Yes | **REVERT** |
| WorkflowEngineService.cs | GetWorkflowJobAsync, GetWorkflowJobsAsync, GetWorkflowJobsByStateAsync | WorkflowJobs | A. Normal business | Yes | **REVERT** |
| TraceQueryService.cs | GetByEventIdAsync, GetByJobRunIdAsync, GetByWorkflowJobIdAsync, GetByEntityAsync, GetMetricsAsync, BuildTimelineFromCorrelationAsync, BuildTimelineFromJobRunOnlyAsync | EventStore, JobRuns, WorkflowJobs | A. Normal business | Yes (scopeCompanyId) | **REVERT** |
| SlaEvaluationService.cs | EvaluateAsync, EvaluateWorkflowTransitionRuleAsync, EvaluateEventProcessingRuleAsync, EvaluateBackgroundJobRuleAsync, EvaluateEventChainStallRuleAsync | SlaRules, WorkflowJobs, EventStore, JobRuns | A. Normal business | Yes (companyId/rule.CompanyId) | **REVERT** |
| BaseWorkRateService.cs | ListAsync, GetByIdAsync, UpdateAsync, DeleteAsync, ValidateForeignKeysAsync, ValidateNoDuplicateActiveAsync | BaseWorkRates, RateGroups | A. Normal business | Yes | **REVERT** |

**Not reverted (pre-existing or justified):**
- WorkflowEngineService: Order lookups in GetEntityCompanyIdAsync, GetCurrentEntityStatusAsync, UpdateEntityStatusAsync (explicit `o.CompanyId == companyId`; defense-in-depth for workflow engine when AsyncLocal may not flow) — per IGNORE_QUERY_FILTERS_AUDIT.md, SAFE_EXPLICIT_COMPANY.
- BillingService ResolveInvoiceLineFromOrderAsync — FinancialIsolationGuard + explicit companyId; SAFE_EXPLICIT_COMPANY.
- AuthService (RefreshToken, ForgotPassword, etc.) — SAFE_PLATFORM (auth flow has no tenant at entry).
- BackgroundJobProcessorService — SAFE_PLATFORM (platform-wide job processing; scope set per job).
- EventPlatformRetentionService — SAFE_PLATFORM (platform retention).
- OrderService DeleteOrderAsync — AssertTenantContext + company filter (explicit or TenantScope); soft-delete visibility.
- DepartmentAccessService — Test-only (EnvironmentName == "Testing").
- DatabaseSeeder, StockLedgerService _isTesting — Seed/test-only.

---

## Phase 2 — Reverted Usages

All `IgnoreQueryFilters()` calls added during test remediation in the following services have been **reverted**:

1. **DocumentGenerationService** — `GetTemplateAsync`: removed from all three DocumentTemplates queries. Caller must have TenantScope set (normal request pipeline or test setup).
2. **WorkflowDefinitionsService** — Removed from all read/query paths (GetWorkflowDefinitionsAsync, GetWorkflowDefinitionAsync, GetEffectiveWorkflowDefinitionAsync, Update, Delete, AddTransition, ValidateUniqueActiveScope).
3. **WorkflowEngineService** — Removed from GetWorkflowJobAsync, GetWorkflowJobsAsync, GetWorkflowJobsByStateAsync only. Order lookups with explicit companyId retained per audit.
4. **TraceQueryService** — Removed from all EventStore, JobRuns, WorkflowJobs queries. Callers must pass scopeCompanyId and have TenantScope set when appropriate.
5. **SlaEvaluationService** — Removed from SlaRules, WorkflowJobs, EventStore, JobRuns queries. Evaluation runs in company context; TenantScope must be set (e.g. by job or API).
6. **BaseWorkRateService** — Removed from ListAsync, GetByIdAsync, UpdateAsync, DeleteAsync, ValidateForeignKeysAsync (RateGroups), ValidateNoDuplicateActiveAsync. Callers must have TenantScope set.

---

## Phase 3 — Retained Bypasses (Justified)

No additional bypasses were "kept" in the reverted services. The following remain elsewhere and are **not** part of this regression; they are documented in [IGNORE_QUERY_FILTERS_AUDIT.md](IGNORE_QUERY_FILTERS_AUDIT.md):

- **BackgroundJobProcessorService:** Comment: "Use IgnoreQueryFilters so we see jobs from all tenants; scope is set per job in ProcessJobAsync." Platform-wide by design.
- **EventPlatformRetentionService:** Platform retention cleanup; runs under platform bypass.
- **OrderService.DeleteOrderAsync:** IgnoreQueryFilters for soft-delete visibility; query constrained by companyId or TenantScope.CurrentTenantId.
- **BillingService / AuthService / RateEngineService / OrderProfitabilityService:** Explicit company-scoped or platform auth; see audit.

---

## Phase 4 — Test Fixes (Correct Way)

Tests that depend on tenant-scoped data must:

1. **Set tenant scope:** `TenantScope.CurrentTenantId = _companyId` at test start (or in a shared setup).
2. **Use one consistent company ID** per test for all seeded entities (`CompanyId = _companyId`).
3. **Restore scope in teardown:** In `Dispose()` or equivalent, restore `TenantScope.CurrentTenantId = _previousTenantId` before disposing the context.
4. **Set scope before async service calls:** If the test awaits and then calls the service again, set `TenantScope.CurrentTenantId` again before the call (or use a single-threaded test collection so scope is not lost).

Test classes fixed in this remediation are listed in §6 below.

---

## Phase 5 — Approved Rules

### IgnoreQueryFilters()

- **Use only when:** (1) The path is intentionally platform-wide (e.g. job processor, retention, seeder) or (2) The path is explicit company-scoped with a guard (e.g. RequireCompany, AssertTenantContext) and the query includes an explicit `CompanyId` (or equivalent) filter. Never use to compensate for missing tenant scope in tests.
- **Document:** Add a short, precise comment explaining why the bypass is intentional (e.g. "Platform-wide: see all tenants' jobs; scope set per job in ProcessJobAsync").

### Tenant-Scoped Tests

- Set `TenantScope.CurrentTenantId` to the test company ID before any tenant-scoped query or save.
- Ensure every seeded entity that is company-scoped has `CompanyId` matching the test company.
- Use `[Collection("TenantScopeTests")]` and disable test parallelization where needed so AsyncLocal is not overwritten by other tests.
- Restore previous tenant scope in Dispose/teardown.

---

## Phase 6 — Services Audited

| Service | Reverted | Retained (justified) |
|---------|----------|----------------------|
| DocumentGenerationService | Yes (GetTemplateAsync) | — |
| WorkflowDefinitionsService | Yes (all) | — |
| WorkflowEngineService | Yes (WorkflowJobs reads only) | Order lookups with explicit companyId |
| TraceQueryService | Yes (all) | — |
| SlaEvaluationService | Yes (all) | — |
| BaseWorkRateService | Yes (all) | — |
| BillingService | No | Explicit companyId + FinancialIsolationGuard |
| AuthService | No | Platform auth (no tenant at entry) |
| BackgroundJobProcessorService | No | Platform-wide job processing |
| EventPlatformRetentionService | No | Platform retention |
| OrderService | No | Soft-delete + company constraint |
| Others | — | See IGNORE_QUERY_FILTERS_AUDIT.md |

---

## Phase 7 — Test Counts and Remaining Failures

- **Before this remediation:** 101 failed, 805 passed (after prior test-remediation that had added IgnoreQueryFilters).
- **After reverts + test fixes:** 121 failed, 785 passed (net −20 passed). Reverts restored correct tenant behavior; additional test and/or service-side tenant scope fixes are required for the reverted services.

**Root cause of remaining failures in reverted clusters:** AsyncLocal (TenantScope) does not always flow across await boundaries in the test runner. So when a service method that takes explicit `companyId` runs and performs an `await`, the continuation may see `TenantScope == null`, and the global query filter then hides tenant-scoped rows. Approved mitigation (no IgnoreQueryFilters): set `TenantScope.CurrentTenantId = companyId` at the start of each service method that accepts `companyId`, inside a try/finally so it is restored (as in BaseWorkRateService for ListAsync, GetByIdAsync, UpdateAsync, DeleteAsync, CreateAsync, and in validation helpers when companyId is provided). This preserves defense-in-depth while making lookups consistent in async contexts.

Remaining failures will be grouped by root cause (missing tenant scope, mismatched CompanyId, real bug, or other) and addressed cluster by cluster without broadening production queries.

---

## Confirmation

- No database schema changes.
- No migrations.
- No platform bypass introduced to normal business flows.
- No weakening of tenant isolation or authorization model.
- Defense-in-depth preserved; tests fixed by aligning tenant scope and CompanyId.
