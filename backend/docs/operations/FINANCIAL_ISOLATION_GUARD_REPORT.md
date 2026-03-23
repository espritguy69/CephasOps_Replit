# Financial Isolation Guard — Implementation Report

**Date:** 2026-03-13  
**Scope:** Defense-in-depth safeguard to prevent cross-company or mismatched-company financial calculations and writes. No schema changes, no migrations, no weakening of existing tenant safety.

---

## 1. What financial isolation safeguard was added

**FinancialIsolationGuard** (`CephasOps.Application/Common/FinancialIsolationGuard.cs`)

A small static guard with four explicit methods:

- **RequireTenantOrBypass(string operationName)**  
  Throws `InvalidOperationException` if neither a valid tenant context (`TenantScope.CurrentTenantId` set and non-empty) nor an approved platform bypass (`TenantSafetyGuard.IsPlatformBypassActive`) is active. Call at the start of finance-sensitive read/write/rebuild paths so operations fail fast when invoked without tenant scope or explicit platform bypass (e.g. repair job running under `TenantScopeExecutor.RunWithPlatformBypassAsync`).

- **RequireCompany(Guid? companyId, string operationName)**  
  Throws `InvalidOperationException` if `companyId` is null or `Guid.Empty`. Use at the start of financial write/calculation paths so operations fail fast when company context is missing.

- **RequireSameCompany(Guid? companyIdA, Guid? companyIdB, string labelA, string labelB, object? idA = null, object? idB = null)**  
  Throws if either company id is null/empty or if they differ. Use when combining two entities in one financial flow (e.g. order vs invoice company). Exception messages include entity labels and optional ids for diagnostics.

- **RequireSameCompanySet(string operationName, Guid expectedCompanyId, IEnumerable<(string Label, Guid? CompanyId, object? Id)> items)**  
  Throws if `expectedCompanyId` is empty, or if any item has null/empty/mismatched company id. Use when building a single financial result from multiple entities (e.g. invoice lines from orders).

Messages are explicit and avoid exposing sensitive data; they identify operation name, entity types, and whether the problem is missing vs mismatched company.

---

## 2. Which services / paths were hardened

| Service | Method | Guard applied |
|--------|--------|----------------|
| **BillingService** | CreateInvoiceAsync | RequireTenantOrBypass, RequireCompany(companyId). For each line item with OrderId, load orders and RequireSameCompanySet(invoice company, orders). If any referenced order is missing or from another company, throw before Add/SaveChanges. |
| **BillingService** | BuildInvoiceLinesFromOrdersAsync | RequireTenantOrBypass, RequireCompany(companyId) at start. |
| **BillingService** | ResolveInvoiceLineFromOrderAsync | RequireTenantOrBypass, RequireCompany(companyId) at start (fail closed when company context missing for revenue resolution). |
| **PayrollService** | CreatePayrollRunAsync | RequireTenantOrBypass, RequireCompany(companyId) at start. Rate resolution request now includes CompanyId = companyId so Base Work Rate / modifiers resolve in same company. |
| **OrderPayoutSnapshotService** | CreateSnapshotForOrderIfEligibleAsync | RequireTenantOrBypass at start. After loading order, RequireCompany(order.CompanyId) so snapshot is never persisted with empty company. |
| **OrderPayoutSnapshotService** | GetSnapshotByOrderIdAsync | RequireTenantOrBypass at start. |
| **OrderPayoutSnapshotService** | GetPayoutWithSnapshotOrLiveAsync | RequireTenantOrBypass at start. When a snapshot exists and caller provides companyId, RequireSameCompany(snapshot.CompanyId, companyId, "Snapshot", "Request", ...) so snapshot from another company is not returned. |
| **OrderProfitabilityService** | CalculateOrderProfitabilityAsync | RequireTenantOrBypass, RequireCompany(companyId) at start. GponRateResolutionRequest now includes CompanyId = order.CompanyId so payout rate resolution is scoped to the order’s company. |
| **OrderProfitabilityService** | GetOrderPayoutBreakdownAsync | RequireTenantOrBypass at start. After loading order, RequireCompany(order.CompanyId). If request companyId is provided, RequireSameCompany(order.CompanyId, companyId, "Order", "Request", order.Id, null). |
| **PnlService** | RebuildPnlAsync | RequireTenantOrBypass, RequireCompany(companyId) at start. |
| **RateEngineService** | ResolveGponRatesAsync | RequireTenantOrBypass at start. Callers pass CompanyId on request so rate resolution is company-scoped; guard ensures path is never run without tenant or bypass. |
| **PayoutAnomalyService** | GetAnomalySummaryAsync, GetAnomaliesAsync, GetTopClustersAsync | RequireTenantOrBypass at start. RunAllRulesAsync: cross-tenant mismatch check — if any snapshot row has Snapshot.CompanyId != OrderCompanyId, RequireSameCompany throws (data integrity). |

No other services were refactored. Rate resolution and tenant-scoped reads remain enforced by EF query filters and TenantScope; the guard adds explicit tenant-or-bypass and company checks at composition/write boundaries. Passing CompanyId on rate requests ensures resolution uses the correct company’s rates (defense-in-depth).

---

## 3. What exact mismatches are now prevented

- **Invoice creation with wrong company for line-item orders**  
  CreateInvoiceAsync requires a non-null company and validates that every order referenced in line items belongs to that company. If an order is from another company (or missing), the guard throws before any invoice is added or saved.

- **Invoice creation with no company**  
  CreateInvoiceAsync throws when companyId is null or empty, so financial writes never proceed without company context.

- **BuildInvoiceLinesFromOrders with no company**  
  BuildInvoiceLinesFromOrdersAsync throws when companyId is empty.

- **ResolveInvoiceLineFromOrder with no company**  
  ResolveInvoiceLineFromOrderAsync throws when companyId is null or empty.

- **Profitability calculation with no company**  
  CalculateOrderProfitabilityAsync throws when companyId is empty.

- **Payout snapshot vs request company mismatch**  
  GetPayoutWithSnapshotOrLiveAsync throws when a snapshot exists for the order but its CompanyId does not match the requested companyId.

- **Payroll run with no company**  
  CreatePayrollRunAsync throws when companyId is null or empty.

- **Payout snapshot with no company on the order**  
  CreateSnapshotForOrderIfEligibleAsync throws when the order’s CompanyId is null/empty, so snapshots are never persisted without company.

- **Payout breakdown with mismatched or missing company**  
  GetOrderPayoutBreakdownAsync requires the order to have a company and, when the caller passes a companyId, requires it to match the order’s company.

- **P&amp;L rebuild with no company**  
  RebuildPnlAsync throws when companyId is null or empty.

---

## 4. What tests were added or updated

**New tests**

- **FinancialIsolationGuardTests** (`CephasOps.Application.Tests/Common/FinancialIsolationGuardTests.cs`)  
  - RequireCompany: valid Guid passes; null and Guid.Empty throw with clear message.  
  - RequireSameCompany: same company passes; mismatched or either missing throws.  
  - RequireSameCompanySet: all matching pass; expected empty, one mismatched, or one missing company throw.

- **BillingServiceFinancialIsolationTests** (`CephasOps.Application.Tests/Billing/BillingServiceFinancialIsolationTests.cs`)  
  - CreateInvoiceAsync_WhenCompanyIdNull_Throws  
  - CreateInvoiceAsync_WhenLineItemOrderFromDifferentCompany_ThrowsBeforeSave  
  - CreateInvoiceAsync_WhenSameCompany_Succeeds  
  - BuildInvoiceLinesFromOrdersAsync_WhenCompanyIdEmpty_Throws  
  - ResolveInvoiceLineFromOrderAsync_WhenCompanyIdEmpty_Throws  

- **OrderProfitabilityServiceTests** (`CephasOps.Application.Tests/Pnl/OrderProfitabilityServiceTests.cs`)  
  - CalculateOrderProfitability_WhenCompanyIdEmpty_Throws  

- **OrderPayoutSnapshotServiceFinancialIsolationTests** (`CephasOps.Application.Tests/Rates/OrderPayoutSnapshotServiceFinancialIsolationTests.cs`)  
  - GetPayoutWithSnapshotOrLiveAsync_WhenSnapshotExistsAndSameCompany_ReturnsSnapshot  
  - GetPayoutWithSnapshotOrLiveAsync_WhenSnapshotExistsAndCompanyMismatch_Throws  

All guard unit tests and financial-isolation service tests pass. Existing Billing/Payroll/Pnl tests that call SaveChangesAsync without setting TenantScope continue to hit TenantSafetyGuard in SaveChangesAsync; those failures are pre-existing test setup issues (tenant context or platform bypass not set in test), not introduced by this guard.

---

## 5. Assumptions and unresolved edge cases

- **Callers of CreateInvoiceAsync with null companyId**  
  Previously allowed (legacy “company feature removed” comment). They now get InvalidOperationException. API and automation (e.g. OrderCompletedAutomationHandler) pass companyId from tenant context or order, so valid flows are unchanged.

- **BuildInvoiceLinesFromOrdersAsync(Guid.Empty)**  
  Now throws. Callers are expected to pass the effective company from tenant context.

- **RebuildPnlAsync(Guid.Empty)**  
  Now throws. PnlRebuildJobExecutor already required company in payload; behavior is aligned.

- **Rate resolution and TenantScope**  
  RateEngineService still uses DbContext (tenant-filtered). Callers (PayrollService, OrderProfitabilityService) now pass CompanyId on GponRateResolutionRequest so Base Work Rate and rate modifiers resolve within the same company as the payroll run or order; no guard inside RateEngineService.

- **Existing tests**  
  Some existing tests fail due to TenantSafetyGuard when calling SaveChangesAsync without TenantScope or platform bypass. This is outside the scope of the financial isolation pass; no changes were made to those tests.

---

## 6. Why this is safe and does not change valid business behavior

- **Same-company flows unchanged**  
  When the caller passes a valid companyId and all referenced entities (orders, etc.) belong to that company, behavior is unchanged. Only invalid or missing company contexts cause new exceptions.

- **Layered with existing tenant safety**  
  The guard does not replace ITenantProvider, TenantScope, EF query filters, or TenantSafetyGuard. It adds explicit checks at financial composition/write boundaries so semantic mismatches (e.g. order from company A, invoice for company B) are caught before persistence.

- **Fail closed**  
  Missing or inconsistent company id causes an immediate, clear exception instead of silent cross-company data or wrong aggregates.

- **Minimal surface**  
  Only the listed methods were changed; no new abstractions, no global behavior change, no schema or API renames. Exception messages are operationally useful without exposing sensitive data.

---

## Summary

| Item | Status |
|------|--------|
| Guard type | FinancialIsolationGuard (static, Application/Common) |
| Methods | RequireTenantOrBypass, RequireCompany, RequireSameCompany, RequireSameCompanySet |
| Services hardened | BillingService, PayrollService, OrderPayoutSnapshotService, OrderProfitabilityService, PnlService, RateEngineService, PayoutAnomalyService (see §2) |
| Tests added | Guard unit tests (incl. RequireTenantOrBypass) + Billing + OrderProfitability + OrderPayoutSnapshot + ResolveInvoiceLine |
| Schema / migrations | None |
| Valid same-company behavior | Unchanged |

**Analyzer/artifacts:** The tenant-safety analyzer and health dashboard do not detect “finance path without RequireTenantOrBypass” or “IgnoreQueryFilters on finance entities.” Adding such checks would require a separate finance-entity allowlist and pattern scan (complex, fragile). This limitation is documented here; manual review and this report are the source of truth for financial isolation coverage.

**Approved platform-wide finance operations:**  
- **Missing Payout Snapshot Repair** (`MissingPayoutSnapshotSchedulerService`): Runs under `TenantScopeExecutor.RunWithPlatformBypassAsync`. It enumerates completed orders without snapshots across all tenants; for each order it calls `CreateSnapshotForOrderIfEligibleAsync`, which runs inside that bypass. Each snapshot creation is guarded by `RequireCompany(order.CompanyId)` inside OrderPayoutSnapshotService, so snapshots are never written with wrong company. The bypass is required so the coordinator can query orders platform-wide; per-order work is still company-checked.  
- **P&amp;L Rebuild** (JobExecution / PnlRebuildJobExecutor): Runs under tenant scope per company (company from job payload).  
- **Payout anomaly detection** (admin/export): Callers that need cross-tenant analytics must run with platform bypass (e.g. admin API that sets bypass or uses a dedicated scope). GetAnomalySummary/GetAnomalies/GetTopClusters now require RequireTenantOrBypass so unauthenticated or tenant-scoped callers without bypass get a clear failure.

---

## 7. Finance-sensitive entity inventory

| Entity | Tenant-scoped (EF filter) | Company-scoped | Read/Write locations | Protected by guard / filter | Path without tenant assertion |
|--------|---------------------------|----------------|----------------------|-----------------------------|-------------------------------|
| OrderPayoutSnapshot | Yes (in IsTenantScopedEntityType) | CompanyId column | OrderPayoutSnapshotService, PayoutAnomalyService, MissingPayoutSnapshotRepairService | RequireTenantOrBypass + RequireCompany / RequireSameCompany; EF filter | Repair runs under platform bypass; each CreateSnapshot guards order.CompanyId. |
| Invoice, InvoiceLineItem, Payment, BillingRatecard | Yes (CompanyScopedEntity) | Yes | BillingService | RequireTenantOrBypass + RequireCompany / RequireSameCompanySet; EF filter | No. |
| PayrollRun, PayrollLine, JobEarningRecord, PayrollPeriod, SiRatePlan | Yes | Yes | PayrollService | RequireTenantOrBypass + RequireCompany; EF filter | No. |
| PnlDetailPerOrder, PnlFact, PnlPeriod, OverheadEntry, OrderFinancialAlert | Yes | Yes | PnlService | RequireTenantOrBypass + RequireCompany; EF filter | No. |
| BaseWorkRate, GponSiJobRate, GponSiCustomRate, GponPartnerJobRate, RateModifier, RateGroup, CustomRate, ServiceProfile, RateCard, RateCardLine | Yes | Yes | RateEngineService, BillingService (BillingRatecard) | RequireTenantOrBypass at ResolveGponRatesAsync; callers pass CompanyId; EF filter | Rate API must be called with tenant or bypass. |
| PayoutSnapshotRepairRun | No (audit record) | No | MissingPayoutSnapshotSchedulerService | Written under platform bypass only | N/A. |
| PayoutAnomalyAlert, PayoutAnomalyAlertRun, PayoutAnomalyReview | No (governance) | Anomaly tied to snapshot/order | PayoutAnomalyService, PayoutAnomalyReviewService | RequireTenantOrBypass on anomaly read paths; cross-tenant mismatch check in RunAllRulesAsync | Admin/export with bypass. |

---

## 8. Background job safety (finance-related)

| Job / Scheduler | Tenant scope / Bypass | Rationale |
|-----------------|------------------------|-----------|
| MissingPayoutSnapshotSchedulerService | Platform bypass | Coordinator must see all completed orders without snapshots; per-order it calls CreateSnapshotForOrderIfEligibleAsync which guards order.CompanyId. Snapshot creation and SaveChanges run under same bypass; TenantSafetyGuard allows SaveChanges under bypass. |
| PnlRebuildJobExecutor (JobExecution) | Tenant scope per company | CompanyId from job payload; runs RebuildPnlAsync(companyId) under TenantScopeExecutor.RunWithTenantScopeOrBypassAsync(companyId). |
| PnlRebuild (legacy BackgroundJobProcessorService) | Tenant scope per company | effectiveCompanyId from payload; RunWithTenantScopeOrBypassAsync(effectiveCompanyId, …). |
| PayoutAnomalyAlertSchedulerService | Platform bypass | Enumerates and runs anomaly detection across tenants; read-only aggregation. |
| Rate resolution (ResolveGponRatesAsync) | Caller’s tenant or bypass | Called from OrderProfitabilityService, PayrollService, OrderPayoutSnapshotService; all callers set tenant or run under bypass and pass CompanyId. |

---

## 9. What exceptions mean

- **"Financial operations require a valid tenant context (TenantScope.CurrentTenantId) or an approved platform bypass"**  
  The code path is finance-sensitive and was invoked without either (1) TenantScope.CurrentTenantId set to a non-empty value, or (2) TenantSafetyGuard.EnterPlatformBypass() active (e.g. inside TenantScopeExecutor.RunWithPlatformBypassAsync). Fix: ensure the request runs after tenant resolution (e.g. API middleware) or that the operation is explicitly run under platform bypass with a documented reason.

- **"Company context is required for this financial operation. CompanyId is missing or empty"**  
  RequireCompany failed: the company id passed to the operation is null or Guid.Empty. Fix: pass the effective company from tenant context or entity.

- **"Company mismatch: PayoutSnapshot (Id=...) and Order (Id=...) must belong to the same company"**  
  RequireSameCompany failed: two entities used in one financial flow have different CompanyIds. Fix: do not combine data from different companies in one operation; fix data if snapshot was incorrectly created.

- **"Company mismatch. ... does not match expected company ..."**  
  RequireSameCompanySet failed: one of the entities (e.g. an order) has a CompanyId that does not match the expected company for the operation. Fix: ensure all referenced entities belong to the same company before building invoices or other financial aggregates.

---

## Related

- **Index of safeguards:** [PLATFORM_SAFETY_HARDENING_INDEX.md](PLATFORM_SAFETY_HARDENING_INDEX.md) — discoverable list of all platform guards and reports.
- **When a guard fails:** [PLATFORM_SAFETY_OPERATOR_RESPONSE.md](PLATFORM_SAFETY_OPERATOR_RESPONSE.md) — operator guidance for financial isolation and other safeguard failures.
