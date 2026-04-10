# GPON Rate Engine — Dimensions Audit & Hardening Report

**Date:** 2026-03-08  
**Scope:** Order Type, Order Category, Installation Method — consistency across rate engine, SI rates, partner rates, payroll, billing, and settings.  
**Constraint:** Audit only; no UI redesign; no code changes in this deliverable.

---

## 1. FINAL DEFINITIONS

| Dimension | Definition | Where used |
|-----------|------------|------------|
| **Order Type** | Job/work type (e.g. ACTIVATION, MODIFICATION, ASSURANCE, VALUE_ADDED_SERVICE). Stored as `OrderTypeId` (FK to OrderTypes). Subtype hierarchy may affect which id is stored; rates use the final OrderTypeId (parent or subtype). | Order, GponSiJobRate, GponPartnerJobRate, GponSiCustomRate, BillingRatecard, Payroll rate resolution, Create Order. |
| **Order Category** | Service/product/technology category (e.g. FTTH, FTTO, FTTR, FTTC, TIME-FTTH). Stored as `OrderCategoryId` (FK to OrderCategories). Previously named "Installation Type" in legacy code. | Order (nullable), GponSiJobRate, GponPartnerJobRate, GponSiCustomRate, Rate resolution, Create Order. **Not** used in BillingRatecard (see ServiceCategory below). |
| **Installation Method** | Method/environment of installation (e.g. PRELAID, NON_PRELAID, SDU, RDF_POLE). Stored as `InstallationMethodId` (FK to InstallationMethods). | Order (nullable), GponSiJobRate, GponPartnerJobRate, GponSiCustomRate, BillingRatecard, Buildings, rate resolution, Create Order. |

---

## 2. RATE ENGINE DIMENSIONS

### SI rates (GponSiJobRate / GponSiCustomRate)

- **Dimensions:** OrderTypeId, OrderCategoryId, InstallationMethodId (optional), SiLevel, PartnerGroupId (optional).
- **Required:** OrderTypeId, OrderCategoryId, SiLevel.
- **Optional:** InstallationMethodId (null = “all methods”), PartnerGroupId (null = default payout).
- **Legacy:** None in domain/EF; DB and API use OrderCategoryId. API query param for filtering is still named `installationTypeId` (see Confusion).

### Partner rates — revenue (GponPartnerJobRate)

- **Dimensions:** PartnerGroupId, PartnerId (optional), OrderTypeId, OrderCategoryId, InstallationMethodId (optional).
- **Required:** PartnerGroupId, OrderTypeId, OrderCategoryId.
- **Optional:** PartnerId (channel override), InstallationMethodId.
- **Legacy:** Same as above; API uses `installationTypeId` query param for OrderCategoryId filter.

### Billing rates (BillingRatecard)

- **Dimensions:** CompanyId, DepartmentId (optional), PartnerGroupId (optional), PartnerId (optional), OrderTypeId (optional), **ServiceCategory (string)**, InstallationMethodId (optional).
- **Required:** None as a set; typical usage is Partner + OrderType + ServiceCategory + InstallationMethod.
- **Optional:** DepartmentId, PartnerGroupId, PartnerId, OrderTypeId, InstallationMethodId.
- **Legacy:** **ServiceCategory is string** (e.g. "FTTH", "FTTO") — same semantic as Order Category but **not** OrderCategoryId (Guid). So billing is inconsistent with SI/Partner rate tables: same concept is FK elsewhere, string here. No automatic mapping from Order.OrderCategoryId to BillingRatecard.ServiceCategory when resolving invoice line amounts.

### Summary

| Rate type | Order Type | Order Category | Installation Method | Other |
|-----------|------------|----------------|---------------------|--------|
| SI (GponSiJobRate) | ✅ Required | ✅ Required | Optional | SiLevel required; PartnerGroupId optional |
| Partner (GponPartnerJobRate) | ✅ Required | ✅ Required | Optional | PartnerGroupId required; PartnerId optional |
| Billing (BillingRatecard) | Optional | ❌ **ServiceCategory (string)** | Optional | Department, Partner, PartnerGroup |

### Rate Resolution Keys

This section defines the **official rate resolution keys** used by the rate engine for each rate type.

---

#### SI payout — resolution order (Phase 3)

GPON payout is resolved in this order (first match wins):

1. **GponSiCustomRate** — per–service installer override (highest priority).
2. **BaseWorkRate** — Rate Group–based rate with dimension fallback (Phase 3).
3. **GponSiJobRate** — legacy default by SI level (lowest priority).

**BaseWorkRate lookup:**

- **RateGroupId** comes from **OrderTypeSubtypeRateGroup** (order type/subtype → rate group).
- **OrderTypeId** in the request is the *leaf* type (parent or subtype); the engine resolves parent vs subtype and then looks up the mapping.
- Dimensions: OrderCategoryId, InstallationMethodId (optional), OrderSubtypeId (optional; for subtype override).
- **Fallback hierarchy** (most specific to least):
  - (a) RateGroup + OrderCategory + InstallationMethod + OrderSubtype  
  - (b) RateGroup + OrderCategory + InstallationMethod  
  - (c) RateGroup + OrderCategory + OrderSubtype  
  - (d) RateGroup + OrderCategory  
  - (e) RateGroup only  
- Effective date and Priority apply; highest priority wins among matches. Results are cached (e.g. 5 min) to limit DB hits.

**Legacy SI payout key (GponSiJobRate / GponSiCustomRate):**

- OrderTypeId  
- OrderCategoryId  
- InstallationMethodId (optional)  
- PartnerGroupId (optional)  
- SI Level  

**Notes:**

- OrderTypeId and OrderCategoryId are required for resolution.
- InstallationMethodId may be null to represent "all installation methods".
- PartnerGroupId may be null for default payout.
- SI Level is used only when falling back to GponSiJobRate.

---

#### Partner revenue (GponPartnerJobRate)

**Resolution key:**

- OrderTypeId  
- OrderCategoryId  
- InstallationMethodId (optional)  
- PartnerGroupId (required)  
- PartnerId (optional override)  

**Notes:**

- PartnerGroupId is required.
- PartnerId may override the group rate.
- InstallationMethodId may be null to apply to all methods.

---

#### Billing ratecards (BillingRatecard)

**Current resolution key:**

- ServiceCategory (string)  
- OrderTypeId (optional)  
- InstallationMethodId (optional)  
- PartnerGroupId (optional)  
- PartnerId (optional)  

**Important note:**  
ServiceCategory is currently a string equivalent of OrderCategory.Code.

**Future improvement:**  
BillingRatecard may eventually adopt OrderCategoryId instead of ServiceCategory.

---

#### Resolution priority

**GPON payout (Phase 3):**

1. GponSiCustomRate (if ServiceInstallerId provided and a custom rate exists)
2. BaseWorkRate (if Rate Group mapping exists and a matching BaseWorkRate row exists)
3. GponSiJobRate (legacy default by SI level)

**Partner revenue** is unchanged: the rate engine still resolves using the most specific matching record (e.g. PartnerId → PartnerGroupId → default).

This logic is implemented in:

- **RateEngineService**
  - `ResolveGponRevenueRateInternalAsync` (partner revenue — unchanged)
  - Payout: `ResolveGponCustomRateAsync` → `ResolveBaseWorkRateAsync` (with cache) → `ResolveGponPayoutRateInternalAsync` (GponSiJobRate)

---

## 3. FILE-BY-FILE AUDIT

### Backend — Domain & persistence

| File | Purpose | Dimensions used | Status |
|------|---------|-----------------|--------|
| `backend/src/CephasOps.Domain/Rates/Entities/GponSiJobRate.cs` | SI payout rate entity | OrderTypeId, OrderCategoryId, InstallationMethodId | ✅ Correct |
| `backend/src/CephasOps.Domain/Rates/Entities/GponPartnerJobRate.cs` | Partner revenue rate entity | OrderTypeId, OrderCategoryId, InstallationMethodId | ✅ Correct |
| `backend/src/CephasOps.Domain/Rates/Entities/GponSiCustomRate.cs` | SI custom rate entity | OrderTypeId, OrderCategoryId, InstallationMethodId | ✅ Correct |
| `backend/src/CephasOps.Domain/Billing/Entities/BillingRatecard.cs` | Billing ratecard entity | OrderTypeId, **ServiceCategory (string)**, InstallationMethodId | ⚠️ Inconsistent: ServiceCategory vs OrderCategoryId |
| `backend/src/CephasOps.Domain/Orders/Entities/Order.cs` | Order entity | OrderTypeId, OrderCategoryId (nullable), InstallationMethodId (nullable) | ✅ Correct |
| `backend/src/CephasOps.Domain/Orders/Entities/OrderCategory.cs` | Order category (FTTH, FTTO, etc.) | N/A | ✅ Correct (comment fixed in Phase 6: SDU/RDF_POLE noted as Installation Methods) |
| `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Rates/GponSiJobRateConfiguration.cs` | EF config | OrderTypeId, OrderCategoryId, InstallationMethodId | ✅ Correct |
| `backend/src/CephasOps.Infrastructure/Persistence/Configurations/Rates/GponPartnerJobRateConfiguration.cs` | EF config | (same) | ✅ Correct |
| `backend/src/CephasOps.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs` | Snapshot | OrderCategoryId on GponSiJobRate, GponPartnerJobRate | ✅ Correct (no InstallationTypeId) |

### Backend — Rate engine & payroll

| File | Purpose | Dimensions used | Status |
|------|---------|-----------------|--------|
| `backend/src/CephasOps.Application/Rates/Services/RateEngineService.cs` | Rate resolution | OrderTypeId, OrderCategoryId, InstallationMethodId (all from request) | ✅ Correct |
| `backend/src/CephasOps.Application/Rates/Services/IRateEngineService.cs` | Rate engine interface | Same | ✅ Correct |
| `backend/src/CephasOps.Application/Rates/DTOs/RateResolutionRequest.cs` | GponRateResolutionRequest | OrderTypeId, OrderCategoryId, InstallationMethodId | ✅ Correct; comment "Previously known as InstallationTypeId" |
| `backend/src/CephasOps.Application/Payroll/Services/PayrollService.cs` | Payroll calculation | Passes order.OrderTypeId, order.OrderCategoryId ?? **Guid.Empty**, order.InstallationMethodId to ResolveGponRatesAsync | ⚠️ **Risk:** OrderCategoryId null → Guid.Empty; no rate match → payout 0 for orders missing OrderCategoryId |
| `backend/src/CephasOps.Application/Billing/Services/BillingRatecardService.cs` | Billing ratecard CRUD | OrderTypeId, ServiceCategory (string), InstallationMethodId | ⚠️ ServiceCategory only; no OrderCategoryId |
| `backend/src/CephasOps.Application/Billing/Services/BillingService.cs` | Invoice create/update | Line item UnitPrice/Quantity from DTO; no BillingRatecard lookup by order dimensions | ⚠️ No automatic resolution of line amount from Order + BillingRatecard |

### Backend — API

| File | Purpose | Dimensions used | Status |
|------|---------|-----------------|--------|
| `backend/src/CephasOps.Api/Controllers/RatesController.cs` | GPON rates & resolve | GET partner/SI: query param **installationTypeId** filters by **OrderCategoryId**. Resolve: body uses GponRateResolutionRequest (OrderCategoryId). | ⚠️ **Legacy naming:** Query param `installationTypeId` means OrderCategoryId |
| `backend/src/CephasOps.Api/Controllers/BillingRatecardController.cs` | Billing ratecards | OrderTypeId, serviceCategory, installationMethodId | Consistent with entity (ServiceCategory string) |
| `backend/src/CephasOps.Api/Controllers/InstallationTypesController.cs` | Legacy alias | Delegates to **OrderCategoryService** (same as Order Categories) | ⚠️ **Confusion:** Route name "InstallationTypes" but implements Order Categories |

### Backend — Orders

| File | Purpose | Dimensions used | Status |
|------|---------|-----------------|--------|
| `backend/src/CephasOps.Application/Orders/Services/OrderService.cs` | Create order, create from draft | CreateOrderAsync: sets OrderCategoryId, InstallationMethodId from dto. **CreateFromParsedDraftAsync:** does **not** set OrderCategoryId or InstallationMethodId on Order | ❌ **Gap:** Orders created from parsed draft have null OrderCategoryId and null InstallationMethodId → payroll/rate resolution will fail or use Guid.Empty |
| `backend/src/CephasOps.Application/Orders/DTOs/OrderDto.cs` | CreateOrderDto, OrderDto | OrderCategoryId, InstallationMethodId on CreateOrderDto and OrderDto | ✅ Correct |
| `backend/src/CephasOps.Application/Orders/Services/OrderCategoryService.cs` | Order category CRUD | OrderCategory entity | ✅ Correct |

### Frontend — Rate pages

| File | Purpose | Dimensions used | Status |
|------|---------|-----------------|--------|
| `frontend/src/pages/settings/RateEngineManagementPage.tsx` | SI/Partner/Custom rates + rate calculator | orderCategoryId, installationMethodId in forms; **resolveRates** called with **installationTypeId** (and no orderCategoryId in body) | ❌ **Bug:** Rate calculator sends `installationTypeId` instead of `orderCategoryId`; backend expects `orderCategoryId` → resolution uses default/empty for category |
| `frontend/src/pages/settings/PartnerRatesPage.tsx` | Billing ratecards (Partner rates) | orderTypeId, **serviceCategory** (string), installationMethodId | ✅ Matches BillingRatecard (ServiceCategory string) |
| `frontend/src/pages/settings/SiRatePlansPage.tsx` | SI rate plans (KPI/bonus) | installationMethodId | ✅ Correct |
| `frontend/src/pages/settings/OrderCategoriesPage.tsx` | Order categories CRUD | Order categories only | ✅ Correct |
| `frontend/src/types/rates.ts` | Rate types | orderCategoryId in GponPartnerJobRateFilters, GponSiJobRateFilters, RateResolutionRequest | ✅ Correct |
| `frontend/src/api/rates.ts` | Rate API | Passes request as-is; filters use orderCategoryId in types | ✅ Correct (backend GET still uses installationTypeId query param name) |

### Frontend — Create Order & orders

| File | Purpose | Dimensions used | Status |
|------|---------|-----------------|--------|
| `frontend/src/pages/orders/CreateOrderPage.tsx` | Create order form | orderCategoryId, installationMethodId in schema and submit | ✅ Correct; sends to CreateOrderDto |
| `frontend/src/types/orders.ts` | Order types | orderCategoryId, installationMethodId | ✅ Correct |

### Frontend — Other

| File | Purpose | Dimensions used | Status |
|------|---------|-----------------|--------|
| `frontend/src/pages/settings/BuildingsPage.tsx` | Buildings | installationMethodId (building’s default installation method) | ✅ Correct |
| `frontend/src/pages/settings/KpiProfilesPage.tsx` | KPI profiles | orderType (orderTypeId) | ✅ Correct |
| `frontend/src/pages/settings/EscalationRulesPage.tsx` | Escalation rules | orderType (display) | ✅ Correct |

---

## 4. CONFUSION / LEGACY ISSUES

| Location | Current label/property | Intended meaning | Recommended |
|----------|------------------------|------------------|-------------|
| **RatesController** GET `/api/rates/gpon/partner-rates` | Query param `installationTypeId` | Order Category (Guid) | Add `orderCategoryId` query param and accept both; deprecate `installationTypeId` or map it to OrderCategoryId. |
| **RatesController** GET `/api/rates/gpon/si-rates` | Query param `installationTypeId` | Order Category (Guid) | Same as above. |
| **InstallationTypesController** | Route/name "InstallationTypes" | Order Categories (FTTH, FTTO, etc.) | Rename to OrderCategories or document as alias for Order Categories; avoid "Installation Type" in UI. |
| **BillingRatecard** entity / BillingRatecardService | **ServiceCategory** (string) | Same concept as Order Category (FTTH, FTTO, etc.) | Prefer adding OrderCategoryId (FK) and migrating; or document that ServiceCategory must match OrderCategory.Code and enforce in validation. |
| **OrderCategory** entity comment | "SDU, RDF_POLE" in list of examples | SDU/RDF_POLE are Installation Methods | Fix comment: Order Category = FTTH, FTTO, FTTR, FTTC (and similar); remove SDU, RDF_POLE. |
| **RateEngineManagementPage** rate calculator | Sends `installationTypeId` in resolve request; validation uses `orderCategoryId` | Order Category for resolution | Send `orderCategoryId: calcRequest.orderCategoryId` in resolveRates body; do not send `installationTypeId` for category. |
| **CreateOrderDto / Order** comment | "Only used for Activation orders" (OrderCategoryId) | Rate engine uses it for all GPON orders | Clarify: for rate resolution, OrderCategoryId is needed whenever GPON rates are keyed by category; recommend always capturing when creating orders. |

---

## 5. SOURCE OF TRUTH

| Area | Source of truth | Notes |
|------|-----------------|--------|
| **Order Types** | Settings: Order Types page; entity OrderType; API `/api/order-types` | Parent/subtype hierarchy; active/inactive. |
| **Order Categories** | Settings: Order Categories page (and legacy Installation Types route); entity OrderCategory; API `/api/order-categories` | Same data as "Installation Types" controller. |
| **Installation Methods** | Settings: Installation Methods page; entity InstallationMethod; API `/api/installation-methods` | Prelaid, Non-Prelaid, SDU, RDF_POLE, etc. |
| **SI rates (GponSiJobRate, GponSiCustomRate)** | Rate Engine Management page; API `/api/rates/gpon/si-rates`, `/api/rates/gpon/si-custom-rates` | Keyed by OrderTypeId, OrderCategoryId, InstallationMethodId, SiLevel. |
| **Partner revenue rates (GponPartnerJobRate)** | Rate Engine Management page; API `/api/rates/gpon/partner-rates` | Keyed by PartnerGroupId, OrderTypeId, OrderCategoryId, InstallationMethodId. |
| **Partner billing rates (BillingRatecard)** | Partner Rates page (Billing ratecards); API `/api/billing-ratecards` | Keyed by OrderTypeId, **ServiceCategory** (string), InstallationMethodId, Partner, Department. |
| **Payroll** | PayrollService uses RateEngineService; reads Order.OrderTypeId, OrderCategoryId, InstallationMethodId | Depends on Order having non-null OrderCategoryId when GPON rates are used. |
| **Invoice line amounts** | Supplied by client in CreateInvoiceDto; no automatic lookup from BillingRatecard by order dimensions in BillingService | No single source of truth for “invoice amount from order”; risk of mismatch with BillingRatecard if done elsewhere. |

---

## 6. RISKS

| Risk | Impact | Likelihood |
|------|--------|------------|
| **Orders created from parsed draft have null OrderCategoryId and null InstallationMethodId** | Payroll uses OrderCategoryId ?? Guid.Empty → no rate match → **zero payout** for those orders. Rate resolution for revenue also wrong. | High if draft-created orders are common. |
| **Rate calculator sends installationTypeId instead of orderCategoryId** | Resolution request has empty OrderCategoryId on server → wrong or no rate returned in Rate Engine Management page. | High when using calculator. |
| **BillingRatecard uses ServiceCategory (string) vs OrderCategoryId (Guid)** | Mapping from Order to BillingRatecard for invoice line amount must use OrderCategory.Code or name; typos or code changes cause wrong or missing billing rates. | Medium. |
| **Invoice line amounts not resolved from BillingRatecard in BillingService** | If another flow builds line items from orders, it must implement its own lookup; inconsistent logic or missing validation. | Medium. |
| **API query param installationTypeId** | Frontends or integrations that send orderCategoryId as param may not match; backend expects installationTypeId for filter. | Low if frontend sends installationTypeId for filters (e.g. Rate Engine Management uses orderCategoryId in types but backend GET uses installationTypeId). |
| **OrderCategory entity comment (SDU, RDF_POLE)** | Developers may add SDU/RDF_POLE as Order Categories; they are Installation Methods → data/rate confusion. | Low. |

---

## 7. RECOMMENDED NEXT STEPS (safest order)

1. **Fix Rate Calculator (frontend)**  
   In `RateEngineManagementPage.tsx` `handleCalculateRate`, pass `orderCategoryId: calcRequest.orderCategoryId` in the `resolveRates` request body and remove or repurpose `installationTypeId` so the backend receives OrderCategoryId. Do not change layout/styling.

2. **Create-from-draft: set OrderCategoryId and InstallationMethodId**  
   In `OrderService.CreateFromParsedDraftAsync`, set `Order.OrderCategoryId` and `Order.InstallationMethodId` from draft or defaults (e.g. from ParsedOrderDraft DTO if added, or from building/order type defaults). Ensures payroll and rate resolution get valid dimensions.

3. **Payroll: handle null OrderCategoryId**  
   When OrderCategoryId is null, either skip rate resolution and log, or use a company/default OrderCategoryId if business rules allow; avoid passing Guid.Empty as if it were a valid category.

4. **API: add orderCategoryId query param**  
   For GET `/api/rates/gpon/partner-rates` and GET `/api/rates/gpon/si-rates`, accept `orderCategoryId` in addition to `installationTypeId` (map both to the same filter) and document `installationTypeId` as deprecated.

5. **BillingRatecard vs Order Category**  
   Document that ServiceCategory must align with OrderCategory.Code (or add OrderCategoryId to BillingRatecard and migrate); add validation when creating/updating BillingRatecard (e.g. ServiceCategory in allowed list or FK to OrderCategory).

6. **OrderCategory entity comment**  
   Remove SDU, RDF_POLE from OrderCategory example list; add a one-line note that Installation Method is a separate dimension.

7. **Invoice line amount resolution (if required)**  
   If business requires invoice line amounts to be derived from orders via BillingRatecard, add a dedicated flow (e.g. “Build lines from orders”) that resolves OrderTypeId, OrderCategory code (from OrderCategoryId), InstallationMethodId, Partner and looks up BillingRatecard; ensure consistent dimension usage and document.

---

## 8. CONFIRMATION

- **CreateOrderPage.tsx layout:** Not modified; audit only.  
- **No UI redesign or styling changes** were made.  
- **Logic and data flow** were audited for dimension consistency only.

---

## 9. IMPLEMENTATION NOTES (post-audit)

The following fixes were implemented per the recommended next steps:

### Phase 1 — Parsed order dimensions
- **CreateOrderFromDraftDto:** Added optional `OrderCategoryId` and `InstallationMethodId`.
- **OrderService.CreateFromParsedDraftAsync:** Resolves OrderCategoryId (required): from dto if valid → else first active OrderCategory for company by DisplayOrder/Name; fails with clear message if unresolved. Resolves InstallationMethodId (optional): from dto if valid → else from building; logs warning if unresolved. Sets both on the created Order.

### Phase 2 — Rate calculator request
- **RateEngineManagementPage.tsx:** `handleCalculateRate` now sends `orderCategoryId: calcRequest.orderCategoryId` in the resolve request body; does not send `installationTypeId` for category. Validation message updated to "Please select order type and order category".

### Phase 3 — Payroll validation
- **PayrollService:** Before rate resolution for each order, throws `InvalidOperationException` if `order.OrderCategoryId` is null or `Guid.Empty`, with message: "Order category must be set before payroll calculation. OrderId=..., ServiceId=...". Uses `order.OrderCategoryId!.Value` after validation.

### Phase 4 — API parameter compatibility
- **RatesController:** GET `gpon/partner-rates` and GET `gpon/si-rates` now accept `orderCategoryId` (preferred) and `installationTypeId` (deprecated). Both are mapped to the same filter via `effectiveOrderCategoryId = orderCategoryId ?? installationTypeId`. Backward compatible.

### Phase 5 — Billing ratecard safety
- **BillingRatecardService:** Create and Update validate that when `ServiceCategory` is non-empty, it must match an existing active `OrderCategory.Code` for the company. Rejects with: "ServiceCategory '...' does not match any active Order Category code. ServiceCategory must equal an existing OrderCategory.Code (e.g. FTTH, FTTO, FTTR, FTTC)." Mapping documented: ServiceCategory ↔ OrderCategory.Code. No schema change.

### Phase 6 — Comment and documentation
- **OrderCategory** entity comment: Removed SDU, RDF_POLE from examples; added note that SDU and RDF_POLE are Installation Methods, not Order Categories.
- **This document:** Added section 9 (Implementation notes).

### Phase 6b — Automatic invoice generation (BillingRatecard)
- **IBillingService / BillingService:** Added `ResolveInvoiceLineFromOrderAsync(orderId, companyId, referenceDate?)` — loads Order (with Partner, OrderCategory), resolves ServiceCategory = OrderCategory.Code, then looks up BillingRatecard with priority: (1) exact partner rate (PartnerId + OrderTypeId + ServiceCategory + InstallationMethodId or null method), (2) partner group rate, (3) department rate, (4) company default. Returns `ResolvedInvoiceLineDto` (OrderId, Description, Quantity=1, UnitPrice, BillingRatecardId) or null if order has no OrderCategoryId or no matching ratecard.
- **BuildInvoiceLinesFromOrdersAsync(orderIds, companyId, referenceDate?):** Returns `BuildInvoiceLinesResult` with `LineItems` (list of `CreateInvoiceLineItemDto`), `UnresolvedOrderIds`, and `Messages`. Does not change existing `CreateInvoiceAsync` / CreateInvoiceDto behaviour.
- **API:** POST `api/billing/invoices/build-lines` with body `{ orderIds, referenceDate? }` returns suggested line items for use in create/update invoice. Company context required.
- **DTOs:** `ResolvedInvoiceLineDto`, `BuildInvoiceLinesResult`, `BuildInvoiceLinesRequest` in InvoiceDto.cs.

### Phase 7 — Tests
- **OrderCreationFromDraftTests:** `ValidateDraft_WhenOrderCategoryIdAndInstallationMethodIdProvided_ReturnsNoErrors` — DTO supports category and method; validation allows them.
- **PayrollServiceOrderCategoryTests:** `CreatePayrollRun_WhenOrderHasNoOrderCategoryId_ThrowsInvalidOperationException` — payroll rejects orders without OrderCategoryId.
- **RateEngineServiceTests:** `ResolveGponRatesAsync_WithOrderCategoryId_ResolvesCorrectRate` — rate resolution uses OrderCategoryId.
- **BillingRatecardServiceTests:** `CreateBillingRatecard_WithUnknownServiceCategory_ThrowsArgumentException`; `CreateBillingRatecard_WithValidServiceCategory_Succeeds`.
- **BillingServiceInvoiceLineTests:** `ResolveInvoiceLineFromOrderAsync_WhenOrderHasCategoryAndMatchingRatecard_ReturnsLine`; `ResolveInvoiceLineFromOrderAsync_WhenOrderHasNoOrderCategoryId_ReturnsNull`; `BuildInvoiceLinesFromOrdersAsync_WithValidOrders_ReturnsLinesAndNoUnresolved`; `BuildInvoiceLinesFromOrdersAsync_WithMixedOrders_ReportsUnresolved`.

### Phase 8 — Documentation (this section)
- **This document:** Added section 10 (Rate Engine Fix Implementation) and diagram below.

### CreateOrderPage
- **CreateOrderPage.tsx layout:** Not modified; no layout or styling changes.

---

## 10. RATE ENGINE FIX IMPLEMENTATION

This section summarizes the implemented fixes and the data flow for payout, revenue, and invoice pricing.

### Parsed order dimension fix
- Orders created from parsed draft now set **OrderCategoryId** (required; from dto or company default) and **InstallationMethodId** (optional; from dto or building). Order creation fails with a clear message if no OrderCategory can be resolved.

### Rate calculator fix
- The rate calculator in Rate Engine Management sends **orderCategoryId** in the resolve request body. The backend uses OrderCategoryId for rate resolution; `installationTypeId` is not used for category in the request.

### Payroll validation rule
- Before resolving SI payout rates, PayrollService validates that **OrderCategoryId** is set (not null, not Guid.Empty). If not, it throws: *"Order category must be set before payroll calculation. OrderId=..., ServiceId=..."*. No silent fallback to zero payout.

### Billing ratecard validation
- When creating or updating a BillingRatecard, **ServiceCategory** must match an existing active **OrderCategory.Code** for the company. Invalid values are rejected with a clear error message.

### Automatic invoice generation flow
- **Build invoice lines from orders:** Call `BuildInvoiceLinesFromOrdersAsync(orderIds, companyId, referenceDate?)` or POST `api/billing/invoices/build-lines` with `{ orderIds, referenceDate? }`.
- For each order, the service loads Order (Partner, OrderCategory), derives ServiceCategory = OrderCategory.Code, and looks up BillingRatecard in priority order: partner → partner group → department → company default (with EffectiveFrom/EffectiveTo respected). Resulting line items have UnitPrice, Description, Quantity=1, OrderId and can be used in CreateInvoice or UpdateInvoice without changing existing invoice create behaviour.

### Diagram: Order → rate resolution and invoice pricing

```
                    Order
                      │
        ┌─────────────┼─────────────┐
        │             │             │
        ▼             ▼             ▼
  RateEngineService   RateEngineService   BillingRatecard
  (SI payout)         (partner revenue)   (invoice price)
        │             │             │
        │             │             │
  OrderTypeId         OrderTypeId   OrderTypeId
  OrderCategoryId     OrderCategoryId  ServiceCategory
  InstallationMethodId  InstallationMethodId  (= OrderCategory.Code)
  PartnerId/GroupId   PartnerId/GroupId  InstallationMethodId
  SiLevel             │             PartnerId / PartnerGroupId
        │             │             DepartmentId / company default
        ▼             ▼             ▼
  GponSiJobRate       GponPartnerJobRate  BillingRatecard.Amount
  / GponSiCustomRate  (revenue)           → invoice line UnitPrice
  (payout)
```

- **Left:** Payroll uses RateEngineService for **payout** (SI rates).
- **Centre:** Revenue reporting uses RateEngineService for **revenue** (partner job rates).
- **Right:** Invoice line amounts can be resolved from **BillingRatecard** using the same dimensions (OrderTypeId, ServiceCategory = OrderCategory.Code, InstallationMethodId, Partner/Group/Department).

### Per-order profitability

Per-order profitability reuses the same resolution:

- **Revenue:** BillingRatecard (same as invoice line resolution).
- **Payout:** RateEngineService (SI payout).

See **docs/ORDER_PROFITABILITY.md** for formula, status (RESOLVED / PARTIAL / UNRESOLVED), reason codes, and API.

---

*End of audit.*
