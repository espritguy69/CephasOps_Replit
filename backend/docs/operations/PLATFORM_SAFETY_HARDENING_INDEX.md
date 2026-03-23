# Platform Safety Hardening — Index

**Date:** 2026-03-13  
**Purpose:** Discoverable index of implemented platform safeguards. Use this page to find detailed reports and protected paths. All listed safeguards are implemented; this index is documentation only.

**Tenant-safety:** Developers should start with the **[TENANT_SAFETY_DEVELOPER_GUIDE.md](../architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md)** for when and how to set scope, bypass rules, PR checklist, and test run; then use [EF_TENANT_SCOPE_SAFETY.md](../architecture/EF_TENANT_SCOPE_SAFETY.md), [TENANT_SAFETY_FINAL_VERIFICATION.md](TENANT_SAFETY_FINAL_VERIFICATION.md), and the related remediation reports below for deeper detail. For a **layered security and tenant-safety architecture diagram** (inputs → resolution → execution → guards → persistence → observability), see **[SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md](../architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md)**. For **when to use global filters vs explicit company-scoped queries** (workflow, profitability, background jobs, tests), see **[TENANT_QUERY_SAFETY_GUIDELINES.md](../architecture/TENANT_QUERY_SAFETY_GUIDELINES.md)**. For the risk that **EF Core relationship fixup** can attach tracked entities from another tenant to navigation properties (bypassing guarded queries), see **[EFCORE_RELATIONSHIP_FIXUP_RISK.md](../architecture/EFCORE_RELATIONSHIP_FIXUP_RISK.md)**.

---

## Safeguards

| Safeguard | Purpose | Key protected paths | Report / reference |
|-----------|---------|---------------------|--------------------|
| **Tenant / auth scope** | Tenant isolation at request and persistence; no tenant-scoped writes without resolved tenant context. | All API requests (TenantGuardMiddleware); SaveChanges (TenantSafetyGuard); global query filters. | **[SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md](../architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md)** (architecture diagram), **[TENANT_SAFETY_DEVELOPER_GUIDE.md](../architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md)** (developer guide), [EF_TENANT_SCOPE_SAFETY.md](../architecture/EF_TENANT_SCOPE_SAFETY.md), [TENANT_QUERY_SAFETY_GUIDELINES.md](../architecture/TENANT_QUERY_SAFETY_GUIDELINES.md) (query patterns), [EFCORE_RELATIONSHIP_FIXUP_RISK.md](../architecture/EFCORE_RELATIONSHIP_FIXUP_RISK.md) (fixup vs guarded queries), [TENANT_SAFETY_ANALYZER.md](TENANT_SAFETY_ANALYZER.md) (Roslyn CEPHAS001–CEPHAS003), [TENANT_SAFETY_FINAL_VERIFICATION.md](TENANT_SAFETY_FINAL_VERIFICATION.md) |
| **Financial isolation** | Payout, billing, and financial operations cannot run with inconsistent or cross-company context. | Payout snapshot creation, rate resolution, ledger operations. | [FINANCIAL_ISOLATION_GUARD_REPORT.md](FINANCIAL_ISOLATION_GUARD_REPORT.md) |
| **EventStore consistency** | Event appends must have valid metadata, company when entity context exists, and consistent stream (same company/entity type per entity stream). | EventStoreRepository.AppendAsync, AppendInCurrentTransaction. | [EVENTSTORE_CONSISTENCY_GUARD_REPORT.md](EVENTSTORE_CONSISTENCY_GUARD_REPORT.md) |
| **Operational observability** | Single internal overview of job executions, event store (24h), payout health, system health, and recent platform guard violations for operators. | Read-only aggregation; guard violations from in-memory buffer (process restart clears). | [OPERATIONAL_OBSERVABILITY_REPORT.md](OPERATIONAL_OBSERVABILITY_REPORT.md). Endpoint: `GET /api/admin/operations/overview`. [OPERATIONAL_OBSERVABILITY_INVENTORY.md](OPERATIONAL_OBSERVABILITY_INVENTORY.md) (what is surfaced vs missing). [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md) (what to do when a safeguard fails). |
| **SI-App workflow (SiWorkflowGuard)** | Order status transitions are restricted to the canonical set; invalid jumps and completion without prior MetCustomer are rejected; reschedule requires reason. | WorkflowEngineService (Order entity); OrderService (reschedule reason). | [SI_APP_WORKFLOW_HARDENING_REPORT.md](SI_APP_WORKFLOW_HARDENING_REPORT.md) |
| **Platform Guardian** | Lightweight detection and reporting layer: scans codebase and health artifacts for tenant, finance, EventStore, workflow, bypass, and artifact-drift patterns; surfaces enforced vs advisory findings. Does not block builds. | N/A (reporting only) | [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md), [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) §7 |
| **Platform Safety Drift Monitor** | Compares current guardian/health output to previous baseline; detects changes in enforced/advisory counts, bypass footprint, and sensitive files requiring review. Surfaces drift for operator review; does not block CI. | N/A (reporting only) | [PLATFORM_SAFETY_DRIFT_REPORT.md](PLATFORM_SAFETY_DRIFT_REPORT.md) |

---

## How to refresh all platform-safety artifacts

From the repository root, one command refreshes the diagram, health dashboard, health JSON/history, and Platform Guardian report:

```powershell
./tools/architecture/regenerate_tenant_safety_artifacts.ps1
```

Commit the updated files if changed: `backend/docs/operations/TENANT_SAFETY_HEALTH_DASHBOARD.md`, `backend/docs/operations/PLATFORM_GUARDIAN_REPORT.md`, `backend/docs/operations/PLATFORM_SAFETY_DRIFT_REPORT.md`, `backend/docs/architecture/SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md`, `tools/architecture/tenant_safety_health.json`, `tools/architecture/tenant_safety_history.json`, `tools/architecture/platform_guardian_report.json`, `tools/architecture/platform_safety_drift_report.json`, `tools/architecture/platform_guardian_baseline.json`. See [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) for CI and local check details.

---

## Safety Layers

Platform safety is enforced at multiple layers; each guard protects a different failure domain.

```
┌─────────────────────────────────────────────────────────────┐
│  API Layer                                                   │
│  • TenantGuardMiddleware (resolves tenant; blocks unscoped)  │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  Application Guards                                         │
│  • SiWorkflowGuard (order transition validity)              │
│  • FinancialIsolationGuard (company-consistent payout/billing)│
│  • EventStoreConsistencyGuard (metadata, company, stream)   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│  Persistence Guard                                          │
│  • TenantSafetyGuard (EF SaveChanges; no tenant-scoped      │
│    writes without valid tenant scope or platform bypass)     │
└─────────────────────────────────────────────────────────────┘
```

---

## Developer Safety Checklist

For tenant-sensitive changes, use the **PR/review checklist** in **[TENANT_SAFETY_DEVELOPER_GUIDE.md](../architecture/TENANT_SAFETY_DEVELOPER_GUIDE.md)** (Section 6). When introducing new background jobs, ingestion flows, or tenant-scoped writes, verify:

- [ ] **TenantScope.CurrentTenantId** is resolved before writing tenant-scoped entities (e.g. set from job payload or middleware).
- [ ] **CompanyId** is propagated at creation time for tenant-owned work (orders, events, snapshots, etc.).
- [ ] **SaveChangesAsync** runs inside a valid tenant scope or a scoped platform bypass (e.g. design-time, seeded data).
- [ ] Any **TenantSafetyGuard.EnterPlatformBypass()** usage is paired with **ExitPlatformBypass()** in a `finally` block.
- [ ] **Null-company** tenant-owned operations fail closed or are explicitly skipped (no silent cross-tenant or orphan data).
- [ ] New tenant-scoped flows include **regression tests** (scope, bypass, and guard behavior).

For **tenant-safety test execution** (stable regression suite, isolated tests, in-memory limitations), see the developer guide Section 7.

---

## Related Remediation Reports

- **[TENANT_SAFETY_CLOSURE_PASS.md](TENANT_SAFETY_CLOSURE_PASS.md)** — Closure pass: developer guide, PR checklist, test guidance, comments, doc review (handoff package).
- **[TENANT_SAFETY_NULL_COMPANY_REMEDIATION.md](../TENANT_SAFETY_NULL_COMPANY_REMEDIATION.md)** — Remediation of null-company and missing-tenant contexts in tenant-owned operations.
- **[TENANT_SAFETY_FINAL_VERIFICATION.md](TENANT_SAFETY_FINAL_VERIFICATION.md)** — Final verification of tenant safety controls and scope enforcement.
- **[TENANT_SCOPE_HARDENING_AUDIT.md](../TENANT_SCOPE_HARDENING_AUDIT.md)** — Audit of tenant-scope hardening across API, persistence, and background jobs.

---

## Workflow and lifecycle docs

- **Order status reference:** `docs/05_data_model/WORKFLOW_STATUS_REFERENCE.md` — 17 order statuses, naming, side paths.
- **Order lifecycle (enforced flow):** `docs/01_system/21_workflow_order_lifecycle.md` — Normal SI path Assigned → OnTheWay → MetCustomer → OrderCompleted; enforcement by SiWorkflowGuard.
- **Seed (DB transitions):** `backend/scripts/postgresql-seeds/07_gpon_order_workflow.sql` — Seeded transitions; application guard aligns with this set.

---

## Guard Violation Logging

Guard failures are logged under the category **PlatformGuardViolation** (constant: `PlatformGuardLogger.CategoryName`) to support operational diagnostics. When a guard is about to throw (TenantSafetyGuard, FinancialIsolationGuard, EventStoreConsistencyGuard, SiWorkflowGuard), a **LogWarning** is emitted with guard name, operation, and safe identifiers (e.g. CompanyId, EntityType, EntityId, EventId). No sensitive payload data is logged. The logger is initialized once at application startup; operators can filter or alert on the category to detect recurring violations.

---

## Related

- **Platform Guardian:** [PLATFORM_GUARDIAN_REPORT.md](PLATFORM_GUARDIAN_REPORT.md) — detection and reporting layer; [TENANT_SAFETY_CI.md](TENANT_SAFETY_CI.md) §7 for how to run and interpret findings.
- **Platform Safety Drift:** [PLATFORM_SAFETY_DRIFT_REPORT.md](PLATFORM_SAFETY_DRIFT_REPORT.md) — compares current run to previous baseline; run after guardian via `regenerate_tenant_safety_artifacts.ps1` or `run_platform_safety_drift.ps1`. Drift does not fail CI; advisory drift is informational only.
- **Operator response:** [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md) — what to do when a safeguard fails.
- **Admin API safety:** `docs/operations/ADMIN_API_SAFETY_VERIFICATION.md` — Authorization, pagination, and scope for admin/ops endpoints (including operations overview).
- **Control plane:** `GET /api/admin/control-plane` lists capability groups; operations overview is listed there as a capability.
