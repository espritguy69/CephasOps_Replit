# Financial Isolation (Payouts, Ratecards, P&L)

**Date:** 2026-03-13  
**Purpose:** Guarantee that all financial calculations are **tenant-scoped and reproducible**, preventing cross-tenant payout contamination. SaaS safety hardening; no business workflow or schema changes.

---

## 1. Tenant payout boundaries

- **Who can see what:** Payout data (ratecards, P&L, order payout snapshots) is visible only in the context of a single company. That context is resolved as **effectiveCompanyId** = `companyId ?? TenantScope.CurrentTenantId`, where `companyId` is the explicit parameter (e.g. from API or job payload) and must not be treated as “all tenants” when empty.
- **Fail closed when context is missing:**
  - **Reads:** If `effectiveCompanyId` is missing (null or `Guid.Empty`), financial read methods return **empty** (empty list, empty summary, or null). No query runs with `CompanyId == null` or `CompanyId == Guid.Empty`.
  - **Writes:** If `effectiveCompanyId` is missing, financial write/rebuild methods throw `InvalidOperationException` with a message that company context is required. No financial write proceeds without a valid company.
- **Cross-tenant:** A request for another tenant’s data (e.g. get ratecard by id with a different company context) returns **null** or empty; snapshot access for an order that belongs to another company returns **null** when the current tenant scope does not match the snapshot’s company.

---

## 2. Ratecard scoping (BillingRatecardService)

- All reads and writes use **effectiveCompanyId** = `(companyId != Guid.Empty ? (Guid?)companyId : null) ?? TenantScope.CurrentTenantId`.
- **Reads** (`GetBillingRatecardsAsync`, `GetBillingRatecardByIdAsync`): When `effectiveCompanyId` is missing, return empty list or null. All queries filter by `effectiveCompanyId.Value`.
- **Writes** (`CreateBillingRatecardAsync`, `UpdateBillingRatecardAsync`, `DeleteBillingRatecardAsync`): Require tenant or approved platform bypass and valid company: `FinancialIsolationGuard.RequireTenantOrBypass(...)` and `FinancialIsolationGuard.RequireCompany(effectiveCompanyId, ...)`. No ratecard is created/updated/deleted without a valid company.
- No query may run with `CompanyId == null` or `CompanyId == Guid.Empty`.

---

## 3. P&L service (PnlService)

- **Read methods** (GetPnlSummaryAsync, GetPnlOrderDetailsAsync, GetPnlDetailPerOrderAsync, GetPnlPeriodsAsync, GetPnlPeriodByIdAsync, GetOverheadEntriesAsync): Use **effectiveCompanyId** as above. When missing, return empty summary, empty list, or null. All queries use `effectiveCompanyId.Value`.
- **Write methods** (CreateOverheadEntryAsync, DeleteOverheadEntryAsync): Require tenant or bypass and valid company; throw when `effectiveCompanyId` is missing.
- **RebuildPnlAsync:** Requires explicit tenant context (or platform bypass) and valid company. Uses `FinancialIsolationGuard.RequireTenantOrBypass("RebuildPnl")` and `FinancialIsolationGuard.RequireCompany(effectiveCompanyId, "RebuildPnl")`. Rebuild labour cost and all P&L queries are scoped by `effectiveCompanyId.Value` (e.g. JobEarningRecords filtered by `CompanyId == effectiveCompanyId.Value`). Rebuild operations must be invoked with an explicit company (e.g. from job payload) and must not run with null or empty company.

---

## 4. Order payout calculations and snapshots

- **OrderPayoutSnapshotService:**
  - **CreateSnapshotForOrderIfEligibleAsync:** Requires tenant or bypass; after loading the order, `FinancialIsolationGuard.RequireCompany(order.CompanyId, ...)` so snapshots are never persisted without company. Snapshot stores order’s company and immutable payout inputs.
  - **GetSnapshotByOrderIdAsync:** Uses **effectiveCompanyId** = `TenantScope.CurrentTenantId`. When missing, returns null. When a snapshot exists but its `CompanyId` does not match `effectiveCompanyId`, returns null (defense-in-depth).
  - **GetPayoutWithSnapshotOrLiveAsync:** Computes **effectiveCompanyId** from parameter and tenant scope. When missing, returns fail-closed response (`Source = "None"`, `Result = Failed("Company context is required for payout resolution.")`). When a snapshot exists, `FinancialIsolationGuard.RequireSameCompany(snapshot.CompanyId, effectiveCompanyId, ...)` so another tenant’s snapshot is never returned. Live resolution uses `effectiveCompanyId` for the profitability call.
- **OrderProfitabilityService:** Already uses `FinancialIsolationGuard.RequireTenantOrBypass` and `RequireCompany(companyId, ...)` and company-scoped order lookup; payout resolution is tenant-scoped.

---

## 5. Payout snapshot protection (immutable inputs)

- Payout calculations that create or use snapshots rely on **immutable snapshots** so that historical payouts remain stable if pricing rules change.
- **OrderPayoutSnapshot** stores:
  - **Ratecard / rule identifiers:** RateGroupId, BaseWorkRateId, ServiceProfileId, CustomRateId, LegacyRateId.
  - **Payout path and resolution:** PayoutPath, ResolutionMatchLevel, BaseAmount, ModifierTraceJson, FinalPayout, ResolutionTraceJson.
  - **Calculation timestamp:** `CalculatedAt` (UTC).
  - **Company and order:** CompanyId, OrderId, InstallerId, Provenance.
- Snapshots are created once per order and not updated after save. Rebuild or repair flows create new snapshots with explicit provenance when needed; they do not mutate existing snapshots.

---

## 6. Reconciliation guarantees

- **Rebuild operations:** P&L rebuild and any financial rebuild require **explicit tenant context** (company from job payload or request). They are executed under `TenantScopeExecutor` (e.g. `RunWithTenantScopeAsync(companyId)` or `RunWithPlatformBypassAsync` for platform repair jobs). Rebuild never runs with `CompanyId == null` or `CompanyId == Guid.Empty` for the target data.
- **Financial exports / reconciliation:** Any export or reconciliation that reads P&L, ratecards, or payouts must use the same **effectiveCompanyId** pattern and fail-closed behavior: no export of financial data without a valid company context; no cross-tenant inclusion in a single export.
- **Audit:** Financial writes (invoices, payments, payout snapshots) log tenantId, entity id, operation, and success so that reconciliation and support can trace which company was used.

---

## 7. Summary of safeguards

| Area | Effective company | Missing context (reads) | Missing context (writes) | Cross-tenant |
|------|-------------------|------------------------|---------------------------|---------------|
| BillingRatecardService | companyId ?? TenantScope.CurrentTenantId | Empty list / null | Throw | Get by id for other tenant → null |
| PnlService (reads) | Same | Empty summary / list / null | — | N/A (queries scoped) |
| PnlService (writes, rebuild) | Same | — | Throw | N/A (queries scoped) |
| OrderPayoutSnapshotService | Same / TenantScope for get-by-order | Get → null; GetPayoutWithSnapshotOrLive → Source=None, Failed result | Create requires company | Snapshot for other tenant → null or throw (RequireSameCompany) |

---

## 8. Related documents

- **TENANT_SAFETY_DEVELOPER_GUIDE.md** — Tenant scope and executor usage.
- **SECURITY_AND_TENANT_SAFETY_ARCHITECTURE.md** — Overall tenant safety architecture.
- **FINANCIAL_ISOLATION_GUARD_REPORT.md** — Guard implementation and test coverage.
- **TENANT_FINANCIAL_SAFETY.md** — Broader financial path audit (invoices, payments, idempotency).

Do not weaken existing tenant guards (TenantSafetyGuard, SiWorkflowGuard, FinancialIsolationGuard, EventStoreConsistencyGuard).
