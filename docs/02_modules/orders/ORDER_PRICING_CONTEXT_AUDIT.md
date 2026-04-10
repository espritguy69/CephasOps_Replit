# Order Pricing Context — Audit Summary

**Date:** 2026-03-08  
**Goal:** Centralize derivation of order pricing context (all pricing-driving fields from an Order) via a shared `IOrderPricingContextResolver`. No change to rate resolution behavior, schema, or pricing rules.

**Implementation status:** Done. `IOrderPricingContextResolver` / `OrderPricingContextResolver` and `OrderPricingContext` added. WorkflowEngineService uses the resolver for Order entities; OrderService uses `IEffectiveScopeResolver.GetOrderTypeCodeForScopeAsync` for workflow order-type code. Billing and rate engines unchanged. Tests added for the resolver.

**Stability (post-refactor pass):** Audited for null handling, company scoping, parent order-type consistency, read-only queries. No behavior drift. Resolver returns null when order not found or wrong company; missing order type yields null OrderTypeCode/ParentOrderTypeCode. See "Future adoption candidates" below for optional downstream use.

---

## 1. Where order pricing context is duplicated

### 1.1 WorkflowEngineService

- **Current:** For Order entities uses `IOrderPricingContextResolver.ResolveFromOrderAsync(entityId, companyId)` for PartnerId, DepartmentId, OrderTypeCode. For other entity types uses `IEffectiveScopeResolver.ResolveFromEntityAsync`.
- **Centralized:** Order path now uses shared pricing context (company-scoped); no duplicate scope logic.

### 1.2 OrderService

- **Current:** In `ApplyStatusChangeAsync` has the order entity; calls private `ResolveOrderTypeCodeForWorkflowAsync(orderEntity.OrderTypeId)` — same logic as EffectiveScopeResolver (OrderTypes.Include(ParentOrderType), parent code when subtype). Builds ExecuteTransitionDto with orderEntity.PartnerId, orderEntity.DepartmentId, orderTypeCode.
- **Duplication:** OrderTypeCode (parent) resolution logic duplicated with EffectiveScopeResolver / future pricing context resolver.

### 1.3 BillingService

- **Current:** `ResolveInvoiceLineFromOrderAsync` loads order with `Include(Partner)`, `Include(OrderCategory)`. Uses order.PartnerId, order.DepartmentId, order.OrderTypeId, order.OrderCategoryId, order.InstallationMethodId, `order.Partner?.GroupId` (PartnerGroupId). Does not use OrderTypeCode (parent); uses OrderTypeId for BillingRatecard match. Priority: Partner → PartnerGroup → Department → General (with InstallationMethodId in line matching).
- **Duplication:** Order load + Partner for GroupId; same set of fields derivable from one place.

### 1.4 RateEngineService

- **Current:** Does not load orders. Callers build `GponRateResolutionRequest` from data they already have (e.g. PayrollService from orders + partner lookup; OrderProfitabilityService from loaded order with Partner).
- **Duplication:** None inside RateEngineService; callers derive PartnerGroupId and other dimensions from order/partner.

### 1.5 PayrollService

- **Current:** Iterates orders (already loaded); looks up partners by order.PartnerId; gets `partnerGroupId = partner?.GroupId`. Builds `GponRateResolutionRequest` with order.OrderTypeId, order.OrderCategoryId, order.InstallationMethodId, partnerGroupId, order.PartnerId, etc.
- **Duplication:** PartnerGroupId derivation (order → partner → GroupId); rest from order.

### 1.6 OrderProfitabilityService

- **Current:** Loads order with `Include(Partner)`, `Include(OrderCategory)`. Uses order.PartnerId, order.OrderCategoryId, order.InstallationMethodId, order.Partner?.GroupId, order.AssignedSiId for rate resolution.
- **Duplication:** Same order+Partner load pattern as BillingService for pricing-related fields.

---

## 2. What will be centralized

- **Single resolver:** `IOrderPricingContextResolver.ResolveFromOrderAsync(orderId, companyId)` returning `OrderPricingContext` with:
  - PartnerId, DepartmentId, OrderTypeId
  - OrderTypeCode (scope: parent when subtype, else own)
  - ParentOrderTypeCode (parent’s Code when subtype, else null)
  - OrderCategoryId, InstallationMethodId
  - PartnerGroupId (from Partner when loaded)
- **One place** for loading Order (with Partner) and OrderType (with ParentOrderType) and projecting these fields.
- **WorkflowEngineService:** For entityType == "Order", use resolver (orderId = entityId, companyId) for PartnerId, DepartmentId, OrderTypeCode instead of/in addition to current scope resolver where applicable.
- **OrderService:** Use shared resolution for OrderTypeCode (e.g. `IEffectiveScopeResolver.GetOrderTypeCodeForScopeAsync` or the new resolver) and remove `ResolveOrderTypeCodeForWorkflowAsync`.

---

## 3. What remains intentionally module-specific

- **Workflow definition priority:** Partner → Department → Order Type → General (unchanged).
- **Billing ratecard priority:** Partner → PartnerGroup → Department → General; line matching by OrderTypeId, ServiceCategory, InstallationMethodId (unchanged). BillingService continues to load the order for its own flow; no forced use of resolver if it would duplicate the load.
- **GPON rate selection:** PartnerId, PartnerGroupId, OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel, etc. (unchanged). RateEngineService and callers keep current behavior; they may optionally use the context DTO as input where it helps.
- **Rate resolution logic:** No change to how any module chooses rate rows or applies fallbacks.

---

## 4. Summary table

| Consumer                 | Today derives                                                                 | Will use resolver for                         | Module-specific (unchanged)                    |
|--------------------------|-------------------------------------------------------------------------------|-----------------------------------------------|-----------------------------------------------|
| WorkflowEngineService    | PartnerId, DepartmentId, OrderTypeCode (via IEffectiveScopeResolver)          | Order path: full context from resolver        | Workflow priority; transition rules            |
| OrderService             | OrderTypeCode (parent) via private method                                     | OrderTypeCode via shared API                  | ApplyStatusChange flow; DTO build              |
| BillingService           | Order + Partner → PartnerId, DepartmentId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId | Optional later; not required this pass        | Ratecard priority; ServiceCategory; line match |
| PayrollService           | order + partners dict → PartnerGroupId; rest from order                       | Optional later                                | SI rate resolution; KPI                        |
| OrderProfitabilityService| order + Partner → same as Billing for payout request                          | Optional later                                | Revenue/payout flow                            |
| RateEngineService        | —                                                                             | —                                             | All rate selection logic                       |

---

## 5. Future adoption candidates (post-merge)

| Module | Recommendation | Notes |
|--------|----------------|-------|
| **BillingService** | Good future adopter | Could call resolver for PartnerId, DepartmentId, OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId and use context for ratecard matching. Would still need OrderCategory code (ServiceCategory) for baseQuery—either add to context later or one lightweight lookup. Avoids duplicate order+Partner load if refactored to use context then load order only for description/category code. |
| **OrderProfitabilityService** | Leave module-specific | Already loads order for revenue (BillingService) and for payout (needs AssignedSiId, AppointmentDate, DocketNumber, etc.). Using resolver would add a second load unless the flow is refactored to context-first; not worth it for now. |
| **PayrollService** | Not worth adopting | Iterates over already-loaded orders with partners in a dictionary. Building GponRateResolutionRequest from in-memory data is cheaper than one resolver call per order. No adoption benefit. |
| **RateEngineService** | Not worth adopting | Does not load orders; callers pass request. No adoption point. |
