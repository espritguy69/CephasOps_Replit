# Per-Order Profitability (GPON)

**Date:** 2026-03-08  
**Scope:** Per-order profitability for GPON orders using BillingRatecard (revenue) and RateEngineService (SI payout).  
**Note:** This is per-order / per-job profitability, not monthly billing.

---

## 1. Formula

**Profit** = **Revenue** − **Payout** − **Material cost** − **Other direct cost**

- **Revenue:** From **BillingRatecard** (invoice line resolution). Same logic as “build invoice lines from orders”.
- **Payout:** From **RateEngineService** (SI payout: GponSiCustomRate or GponSiJobRate).
- **Material cost:** Placeholder; currently **0**. Source to be implemented later (e.g. material usage, templates).
- **Other direct cost:** Optional future extension; currently **0**.

**Minimum viable (current):**

**Profit** = **Billing revenue** − **SI payout**

---

## 2. Revenue source

- **Source:** `IBillingService.ResolveInvoiceLineFromOrderAsync` (BillingRatecard lookup).
- **Rules:**
  - If a BillingRatecard match exists → revenue = UnitPrice (× quantity).
  - If no match → revenue unresolved; reason code set (no silent zero).

**Reason codes (revenue):**

| Code | Meaning |
|------|--------|
| `ORDER_NOT_FOUND` | Order not in company scope. |
| `ORDER_CATEGORY_MISSING` | Order has no OrderCategoryId; required for BillingRatecard. |
| `PARTNER_MISSING` | Order has no partner. |
| `NO_BILLING_RATECARD_FOUND` | No BillingRatecard for order dimensions (partner, order type, service category, installation method). |

---

## 3. SI payout source

- **Source:** `IRateEngineService.ResolveGponRatesAsync` (payout side only).
- **Dimensions:** OrderTypeId, OrderCategoryId, InstallationMethodId, PartnerGroupId (from Partner), ServiceInstallerId (AssignedSiId), SiLevel (from ServiceInstaller).

**Rules:**

- If payout resolved → PayoutAmount and PayoutSource set.
- If not resolved → reason code set; no silent zero.

**Reason codes (payout):**

| Code | Meaning |
|------|--------|
| `ORDER_CATEGORY_MISSING` | Order category required for SI rate resolution. |
| `NO_ASSIGNED_SI` | No AssignedSiId; SI level unknown. |
| `SI_LEVEL_MISSING` | SI level could not be determined. |
| `PARTNER_GROUP_MISSING` | Partner has no GroupId (optional for some rates). |
| `NO_SI_RATE_FOUND` | No GponSiCustomRate or GponSiJobRate for dimensions and SI level. |

**Fallback when order has no completed installer:**  
If there is no AssignedSiId, payout cannot be resolved (no SI level). Result is PARTIAL or UNRESOLVED with reason `NO_ASSIGNED_SI`. No default payout is applied without an SI context.

---

## 4. Status model

| Status | Meaning |
|--------|--------|
| **RESOLVED** | Revenue and payout both found. Profit and margin are calculated. |
| **PARTIAL** | One of revenue or payout found, the other missing. Profit not set (no silent zero for missing side). |
| **UNRESOLVED** | Both missing, or critical dimensions missing (e.g. order not found). |

Each result includes:

- **Messages / Warnings:** Human-readable list.
- **ReasonCodes:** Machine-readable list for reporting and finance review.

---

## 5. Current exclusions

- **Material cost:** Not implemented. Field present, default **0**; design allows a future source (e.g. material usage, templates).
- **Other direct cost:** Optional future extension; currently **0**.

---

## 6. Flow

```
Order
  → BillingRatecard / invoice line resolution (ResolveInvoiceLineFromOrderAsync) → Revenue
  → RateEngineService payout resolution (ResolveGponRatesAsync)                  → Payout
  → OrderProfitabilityService                                                    → Profit, Margin, Status
```

---

## 7. API

- **GET** `/api/orders/{id}/profitability?referenceDate=optional`  
  Single-order profitability.

- **POST** `/api/orders/profitability/bulk`  
  Body: `{ "orderIds": ["...", "..."], "referenceDate": "optional" }`  
  Returns list of `OrderProfitabilityDto`.

**Optional enrichment (order list/drilldown):**

- **GET** `/api/orders/{id}?includeProfitability=true`  
  Returns the order with `RevenueAmount`, `PayoutAmount`, `ProfitAmount` populated from the profitability engine when company context is available.

- **GET** `/api/orders/paged?includeProfitability=true&...`  
  Returns the paged order list with revenue, payout, and profit set on each item. Uses company context when present; when `includeProfitability=true` and the user has a company, results are company-scoped and then enriched.

---

## 8. Service location

- **Interface:** `CephasOps.Application.Pnl.Services.IOrderProfitabilityService`
- **Implementation:** `CephasOps.Application.Pnl.Services.OrderProfitabilityService`
- **DTOs:** `CephasOps.Application.Pnl.DTOs.OrderProfitabilityDto`, `OrderProfitabilityStatus`, `OrderProfitabilityReasonCodes`, `BulkOrderProfitabilityRequest`

---

## 9. Optional order list enrichment

`OrderDto` includes optional fields for future list/drilldown UI (no UI change in this deliverable):

- `RevenueAmount`
- `PayoutAmount`
- `ProfitAmount`

They are not populated by default. A future list or drilldown can call the profitability API and map results onto these fields.

---

## 10. Financial alerts

Profit alerts and negative-margin detection are built on top of profitability. See **docs/ORDER_FINANCIAL_ALERTS.md** for alert codes, severity rules, low-margin threshold, and API (`GET/POST financial-alerts`, optional `includeFinancialAlerts` on order endpoints).
