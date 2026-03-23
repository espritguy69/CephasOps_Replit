# Rate Resolution Scope Audit — Workflow vs Rate Engines

**Date:** 2026-03-08  
**Goal:** Compare rate resolution logic (SI rates, partner rates, billing ratecards, payroll) with the workflow engine’s scoped resolution pattern and determine whether scope resolution should be unified.  
**Deliverable:** Report only; no code changes.

---

## 1. Workflow resolution pattern (reference)

**Source:** `WorkflowDefinitionsService.GetEffectiveWorkflowDefinitionAsync`, `docs/WORKFLOW_RESOLUTION_RULES.md`.

**Resolution order (strict):**

1. **Partner** — `EntityType` + `CompanyId` + `PartnerId` (match) + `IsActive`
2. **Department** — same + `DepartmentId` (match), with `PartnerId == null`
3. **Order type** — same + `OrderTypeCode` (match), with `PartnerId == null`, `DepartmentId == null`
4. **General** — same + `PartnerId == null`, `DepartmentId == null`, `OrderTypeCode == null`

**Scope dimensions:** `PartnerId`, `DepartmentId`, `OrderTypeCode` (string, from parent OrderType.Code when applicable).  
**No** PartnerGroupId in workflow scope.

---

## 2. Current rate resolution by module

### 2.1 GponPartnerJobRate (revenue — what Cephas earns from partner)

**Where:** `RateEngineService.ResolveGponRevenueRateInternalAsync`.

**Resolution order:**

1. **PartnerId** — match `PartnerId`; keys: OrderTypeId, OrderCategoryId, InstallationMethodId, validity.
2. **PartnerGroupId** — match `PartnerGroupId`, `PartnerId == null` on rate.
3. **Default** — no partner filter (query allows PartnerGroupId or unset).

**Scope dimensions used:** `PartnerId`, `PartnerGroupId`.  
**Not used:** DepartmentId, OrderTypeCode (OrderTypeId is part of the match key for every tier, not a scope tier).

**Entity:** `GponPartnerJobRate` has PartnerGroupId (required), PartnerId (optional). No DepartmentId, no OrderTypeCode.

---

### 2.2 GponSiJobRate (default SI payout)

**Where:** `RateEngineService.ResolveGponPayoutRateInternalAsync`.

**Resolution order:**

1. **PartnerGroupId** — match `PartnerGroupId`; keys: OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel.
2. **Default** — `PartnerGroupId == null` on rate.

**Scope dimensions used:** `PartnerGroupId` only.  
**Not used:** PartnerId (no per-partner SI payout tier), DepartmentId, OrderTypeCode.

**Entity:** `GponSiJobRate` has PartnerGroupId (optional). No PartnerId, DepartmentId, or OrderTypeCode.

---

### 2.3 GponSiCustomRate (per-SI custom payout override)

**Where:** `RateEngineService.ResolveGponCustomRateAsync`.

**Resolution:** Single match by ServiceInstallerId + OrderTypeId + OrderCategoryId + optional InstallationMethodId + optional PartnerGroupId (as filter). No multi-tier scope fallback.

**Scope dimensions:** PartnerGroupId only as optional filter. No PartnerId, DepartmentId, or OrderTypeCode tier.

**Entity:** `GponSiCustomRate` has PartnerGroupId (optional). No PartnerId, DepartmentId, OrderTypeCode.

---

### 2.4 BillingRatecard (invoice line unit price)

**Where:** `BillingService.ResolveInvoiceLineFromOrderAsync`.

**Resolution order:**

1. **Partner** — `PartnerId == order.PartnerId`; base filter: OrderTypeId, ServiceCategory, InstallationMethodId, validity.
2. **Partner group** — `PartnerGroupId == order.Partner.GroupId`, `PartnerId == null` on ratecard.
3. **Department** — `DepartmentId == order.DepartmentId` or null; `PartnerId == null`, `PartnerGroupId == null`.
4. **General** — `PartnerId == null`, `PartnerGroupId == null`, `DepartmentId == null`.

**Scope dimensions used:** PartnerId, PartnerGroupId, DepartmentId.  
**Not used as scope tier:** OrderTypeCode / OrderTypeId is part of the base filter for all tiers, not a fallback level.

**Entity:** `BillingRatecard` has DepartmentId, PartnerGroupId, PartnerId, OrderTypeId. No OrderTypeCode string; OrderTypeId is a match dimension, not a scope tier.

---

### 2.5 Payroll (SI payout for job earning records)

**Where:** `PayrollService.CreatePayrollRunAsync` → `IRateEngineService.ResolveGponRatesAsync`.

**Resolution:** No own resolution. Uses:

- **Revenue:** `ResolveGponRevenueRateInternalAsync` (PartnerId → PartnerGroupId → default).
- **Payout:** `ResolveGponCustomRateAsync` then `ResolveGponPayoutRateInternalAsync` (Custom → PartnerGroupId → default).

So payroll inherits the same scope logic as the rate engine: no Department, no Order Type scope tier.

---

## 3. Comparison with workflow resolution

| Aspect | Workflow | GponPartnerJobRate | GponSiJobRate | GponSiCustomRate | BillingRatecard |
|--------|----------|--------------------|---------------|------------------|-----------------|
| **Partner** | ✅ Tier 1 (PartnerId) | ✅ Tier 1 (PartnerId) | ❌ | ❌ (filter only) | ✅ Tier 1 (PartnerId) |
| **Partner group** | ❌ | ✅ Tier 2 | ✅ Tier 1 | Optional filter | ✅ Tier 2 |
| **Department** | ✅ Tier 2 | ❌ | ❌ | ❌ | ✅ Tier 3 |
| **Order type (scope)** | ✅ Tier 3 (OrderTypeCode) | ❌ | ❌ | ❌ | ❌ (OrderTypeId in base filter only) |
| **General** | ✅ Tier 4 | ✅ (default) | ✅ (default) | N/A | ✅ Tier 4 |

**Findings:**

- **Workflow** is the only place that uses **Department** and **OrderTypeCode** as explicit scope tiers (Partner → Department → Order Type → General).
- **Billing** is the only rate path that uses **Department**; order is Partner → Partner Group → Department → General (no Order Type tier).
- **GPON rates** (revenue/payout/custom) use **Partner and/or PartnerGroup** only; no Department, no Order Type scope tier.
- **OrderTypeId** in rates is a **match dimension** (which rate row applies for this order type), not a **scope tier** (fallback level). Workflow’s **OrderTypeCode** is a scope tier (e.g. “use MODIFICATION workflow when no partner/department match”).

---

## 4. Should rate resolution reuse the same mechanism?

**Arguments for alignment:**

- Consistent behaviour for “partner vs department vs order type vs general” across workflows and rates.
- One place to document and test scope rules; fewer surprises when configuring partner/department/order-type-specific rates.
- Billing already has Partner → PartnerGroup → Department → General; aligning with Partner → Department → Order Type → General would require a clear decision on where PartnerGroup and Order Type sit.

**Arguments against full unification:**

- **Different semantics:** Workflow answers “which workflow runs?” (one definition per scope). Rates answer “which rate row applies?” and use extra dimensions (OrderTypeId, OrderCategoryId, SiLevel, InstallationMethodId, ServiceCategory). A single “scope resolver” would only cover Partner/Department/OrderType; rate-specific keys would remain in each engine.
- **PartnerGroup is central in rates:** Revenue and SI payout are structured by PartnerGroup (e.g. TIME, CELCOM_DIGI) and optionally Partner. Workflow has no PartnerGroup. Forcing workflow’s 4-tier model onto rates would require either adding PartnerGroup to workflow or dropping it from rate scope (breaking change).
- **Order Type in workflow vs rates:** In workflow, OrderTypeCode is a scope tier (fallback). In rates, OrderTypeId is a required/match dimension. Adding “Order Type scoped rate” as a tier (between Department and General) would be a new capability, not a direct reuse of workflow logic.
- **Department in GPON rates:** GponSiJobRate / GponPartnerJobRate / GponSiCustomRate have no DepartmentId. Adding it would require schema and data migration and new resolution steps.

**Conclusion:**  
A **shared “scope resolver”** that only returns the effective scope (e.g. Partner / Department / Order Type / General) from (PartnerId, DepartmentId, OrderTypeCode) could be reused by workflow and by rate engines that choose to adopt the same tiers. The **rate engines would not** replace their existing logic with a single call; they would use that scope (and optionally PartnerGroup, OrderTypeId, etc.) to decide which rate row to use. Unification is therefore **partial**: same scope order and semantics where applicable, with rate-specific dimensions and PartnerGroup handled inside each rate module.

---

## 5. Proposed safe, minimal approach (no code changes in this deliverable)

### 5.1 Option A — Shared scope resolver (recommended direction)

- **Add** a small abstraction (e.g. `IScopeResolutionContext` or `IEffectiveScopeResolver`) that:
  - **Inputs:** CompanyId, EntityType (if needed), PartnerId?, DepartmentId?, OrderTypeCode? (and optionally PartnerGroupId for rate callers).
  - **Output:** Effective scope level (e.g. enum: Partner, Department, OrderType, General) and the resolving key (PartnerId, DepartmentId, OrderTypeCode, or none).
- **Workflow:** Keep current behaviour; optionally refactor to use this resolver so workflow and rates share one implementation of “Partner → Department → Order Type → General”.
- **Billing:** Already has Partner → PartnerGroup → Department → General. Either:
  - Keep as-is and document the difference from workflow (PartnerGroup between Partner and Department), or
  - Introduce an “Order Type” tier (e.g. after Department, before General) and optionally align order with workflow (Partner → Department → Order Type → General), with PartnerGroup semantics defined (e.g. “PartnerGroup under Partner” or “replaces Partner”).
- **GPON rates:** No change in behaviour initially. If desired later:
  - Add optional DepartmentId (and optionally OrderTypeCode) to GponPartnerJobRate / GponSiJobRate / GponSiCustomRate and run resolution in order: Partner → Department → Order Type → General (with PartnerGroup handled inside that, e.g. Partner = PartnerId or PartnerGroupId depending on design).

**Safety:** No breaking changes if the resolver is additive and callers adopt it only where they want the same scope order as workflow.

### 5.2 Option B — Document only

- Document current resolution order for each module (workflow, BillingRatecard, GponPartnerJobRate, GponSiJobRate, GponSiCustomRate, payroll) in one place (e.g. this doc or a central “Resolution rules” page).
- Explicitly state where they differ (PartnerGroup, Department, Order Type tier) and leave implementation as-is until a product decision is made to unify.

### 5.3 Option C — Align Billing with workflow order (narrow change)

- Change **only** `BillingService.ResolveInvoiceLineFromOrderAsync` so that after Partner and PartnerGroup, the order is Department → then “Order Type scoped” (if BillingRatecard gains OrderTypeCode or equivalent) → then General.
- This would require a clear definition of “Order Type scoped” for billing (e.g. ratecards with OrderTypeCode set, matching order’s resolved OrderTypeCode) and possibly schema change. No change to GPON rate entities or payroll in this option.

---

## 6. Summary table

| Module | Current scope order | Same as workflow? | Department? | Order type as scope? |
|--------|---------------------|-------------------|------------|----------------------|
| **Workflow** | Partner → Department → Order Type → General | — | ✅ | ✅ |
| **GponPartnerJobRate** | PartnerId → PartnerGroupId → default | No (has PartnerGroup; no Dept/OT) | ❌ | ❌ |
| **GponSiJobRate** | PartnerGroupId → default | No | ❌ | ❌ |
| **GponSiCustomRate** | Single match (+ optional PartnerGroup filter) | No | ❌ | ❌ |
| **BillingRatecard** | PartnerId → PartnerGroupId → Department → General | No (PartnerGroup; no OT tier) | ✅ | ❌ |
| **Payroll** | Delegates to RateEngineService (same as GPON) | No | ❌ | ❌ |

**Recommendation:**  
Use **Option A** in the long term (shared scope resolver + optional adoption by rate engines) and **Option B** immediately (document current behaviour and differences). Option C only if product explicitly wants billing to support an “Order Type” scope tier and to align its order with workflow without changing GPON rates or payroll first.

---

## 7. Files referenced

- **Workflow:** `WorkflowDefinitionsService.cs` (GetEffectiveWorkflowDefinitionAsync), `WORKFLOW_RESOLUTION_RULES.md`
- **Rates:** `RateEngineService.cs` (ResolveGponRevenueRateInternalAsync, ResolveGponPayoutRateInternalAsync, ResolveGponCustomRateAsync), `GponRateResolutionRequest`, `RateResolutionRequest.cs`
- **Billing:** `BillingService.cs` (ResolveInvoiceLineFromOrderAsync), `BillingRatecardService.cs`, `BillingRatecard.cs`
- **Payroll:** `PayrollService.cs` (CreatePayrollRunAsync → ResolveGponRatesAsync)
- **Entities:** `GponSiJobRate.cs`, `GponPartnerJobRate.cs`, `GponSiCustomRate.cs`, `BillingRatecard.cs`
- **Audit:** `docs/GPON_RATE_ENGINE_DIMENSIONS_AUDIT.md`

No code changes were made in this audit.
