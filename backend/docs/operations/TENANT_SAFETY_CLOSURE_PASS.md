# Tenant Safety — Closure Pass

**Date:** 2026-03-12  
**Purpose:** Permanent handoff package after tenant-safety remediation and final verification. No production behavior or unrelated code changed; maintainability and developer guidance only.

**For day-to-day tenant-safety workflow,** use the **[developer guide](../architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md)** as the primary entry point.

---

## 1. Docs updated

| Document | Change |
|----------|--------|
| **[TENANT_SAFETY_DEVELOPER_GUIDE.md](../architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md)** | **Created.** Short authoritative developer guide: when TenantScope must be set, when EnterPlatformBypass is allowed, try/finally restoration pattern, hosted services/dispatchers/replays/webhooks/auth/job workers, fail-closed for null-company, PR checklist (Section 6), test execution (Section 7). |
| **[EF_TENANT_SCOPE_SAFETY.md](../architecture/EF_TENANT_SCOPE_SAFETY.md)** | Added one-line pointer at top: for day-to-day guidance and PR/test run, see TENANT_SAFETY_DEVELOPER_GUIDE.md. |
| **[PLATFORM_SAFETY_HARDENING_INDEX.md](PLATFORM_SAFETY_HARDENING_INDEX.md)** | Tenant row now lists TENANT_SAFETY_DEVELOPER_GUIDE as the primary developer guide; Developer Safety Checklist references the guide’s Section 6 (PR checklist) and Section 7 (test execution). |
| **[09_backend/testing_guidelines.md](../09_backend/testing_guidelines.md)** | New subsection “Tenant-scoped and tenant-safety tests”: tests that add tenant-scoped entities must set TenantScope (or bypass) and restore in finally; TenantScopeTests collection; pointer to developer guide Section 7 for regression suite and limitations. |

---

## 2. Review checklist added

**Location:** [TENANT_SAFETY_DEVELOPER_GUIDE.md](../architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md) **Section 6** — “PR / review checklist for tenant-sensitive changes.”

Checklist items: TenantScope set (or bypass) before SaveChanges; restoration in finally; no new blanket bypass; null-company fail closed/skip; IgnoreQueryFilters only with AssertTenantContext or bypass; tests for new tenant paths. The index links to this section for code review.

---

## 3. Test-run guidance added

**Location:** [TENANT_SAFETY_DEVELOPER_GUIDE.md](../architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md) **Section 7** — “Test execution (tenant-safety regression and limitations).”

- **7.1 Stable tenant-safety regression suite:** Exact `dotnet test --filter ...` command for the 16 tests to run before merging tenant-sensitive changes.
- **7.2 Isolated tests:** Three OrderAssignedOperationsHandlerTests that should be run separately when needed; production behavior is correct (dispatcher sets scope).
- **7.3 In-memory provider and global query filter:** Note on test order/parallelism, TenantScopeTests collection, and setting TenantScope in tests that add tenant-scoped entities.

Also referenced from [testing_guidelines.md](../09_backend/testing_guidelines.md) and from the platform safety index.

---

## 4. Code comments added

| File | Comment |
|------|---------|
| **EventStoreDispatcherHostedService.cs** | XML summary on `ProcessOneEventAsync`: “Dispatches one event: set tenant scope from entry.CompanyId or use platform bypass; restore in finally (tenant-safety).” |
| **JobExecutionWorkerHostedService.cs** | Single line before per-job scope set: “Tenant scope per job so SaveChanges and query filters see this tenant; restored in finally (tenant-safety).” |

No other production files were modified. InboundWebhookRuntime, BackgroundJobProcessorService, NotificationRetentionService, TenantSafetyGuard, and ApplicationDbContext already had sufficient comments from the remediation.

---

## 5. Outdated or duplicate tenant-safety docs

**Reviewed:** TENANT_GUARD_AUDIT_REPORT.md, TENANT_RESOLUTION_AUDIT_REPORT.md, TENANT_SAFETY_NULL_COMPANY_REMEDIATION.md, TENANT_SAFETY_SECOND_PASS_REMEDIATION.md, TENANT_SCOPE_HARDENING_AUDIT.md, TENANT_SAFETY_FINAL_VERIFICATION.md, EF_TENANT_SCOPE_SAFETY.md.

**Conclusion:**

- **No duplicates to delete.** Each doc has a distinct role: guard audit (middleware/resolution), resolution audit (precedence), null-company remediation (per-path fix log), second-pass remediation (second pass log), scope hardening audit (hosted services/write paths), final verification (verification + regression tests), EF doc (persistence layer).
- **Consolidation:** The new **TENANT_SAFETY_DEVELOPER_GUIDE** is the single entry point for “how do I…” and “what do I run for tests.” The other docs remain as references: architecture (EF), audit/verification (final verification, guard audit, resolution audit), and remediation history (null-company, second pass, scope hardening).
- **Recommendation:** Do not merge the remediation/audit docs into the developer guide; keep them for traceability. When linking from the index or README, point to the developer guide first, then to EF_TENANT_SCOPE_SAFETY and TENANT_SAFETY_FINAL_VERIFICATION for detail.

---

## 6. Final maintainability recommendations

1. **Onboarding:** Point new developers to **docs/architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md** for tenant-safety (and to **docs/operations/PLATFORM_SAFETY_HARDENING_INDEX.md** for the full safety index).
2. **PRs:** For changes touching persistence, workflow, event dispatch, webhooks, or job execution, require the **Section 6 checklist** (in the developer guide) and a run of the **Section 7.1 regression suite** (or justification to skip).
3. **New bypass or new hosted service:** Update the developer guide Section 2 (and Section 4 if applicable) and keep Enter/Exit in try/finally; document in the same PR.
4. **Tests:** Any new test that adds tenant-scoped entities should set TenantScope (or bypass) before SaveChanges and restore in finally; if the test class manipulates TenantScope, add `[Collection("TenantScopeTests")]` (see testing_guidelines and developer guide Section 7.3).
5. **Docs:** When adding a new tenant-safety–related doc, add it to the “Related” / “Tenant” section of PLATFORM_SAFETY_HARDENING_INDEX and, if it’s the canonical place for a topic, reference it from the developer guide.

---

**Closure pass complete.** Production behavior unchanged; guardrail clarity and developer guidance improved; handoff package in place.
