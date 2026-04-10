# Order Financial Alerts (GPON)

**Date:** 2026-03-08  
**Scope:** Profit alerts and negative-margin detection for GPON orders, derived from the existing profitability engine.  
**Data source:** OrderProfitabilityService (revenue from BillingRatecard, payout from RateEngineService). Alerts can be **computed live** or **evaluated and persisted** (optional); Critical alerts trigger an optional notification hook.

---

## 1. Flow

```
Order
  → ProfitabilityService
      → Revenue (BillingRatecard / ResolveInvoiceLineFromOrderAsync)
      → Payout (RateEngineService)
      → Profit, Margin, Status
  → ProfitAlertService
      → Alerts (derived from profitability result)
```

---

## 2. Alert codes

| Code | Meaning | Typical severity |
|------|--------|-------------------|
| `NEGATIVE_PROFIT` | Profit &lt; 0 | Critical |
| `PAYOUT_EXCEEDS_REVENUE` | Payout &gt; Revenue | Critical |
| `LOW_MARGIN` | Margin % below configured threshold | Warning |
| `NO_BILLING_RATE_FOUND` | No BillingRatecard for order | Critical |
| `NO_PAYOUT_RATE_FOUND` | No SI payout rate or SI level missing | Warning |
| `ORDER_CATEGORY_MISSING` | Order has no OrderCategoryId | Critical |
| `INSTALLATION_METHOD_MISSING` | Installation method not set; rate may be less specific | Warning |
| `ORDER_TYPE_INACTIVE` | Reserved for future use | — |
| `PROFITABILITY_UNRESOLVED` | Profitability status UNRESOLVED | Critical |

---

## 3. Severity rules

| Severity | When used |
|----------|-----------|
| **Critical** | Negative profit, payout &gt; revenue, missing order category, no billing rate, profitability unresolved |
| **Warning** | Low margin (below threshold), no payout rate, installation method missing |
| **Info** | Reserved for future use |

---

## 4. Low-margin threshold

- **Config key:** `ProfitabilityAlerts:LowMarginThresholdPercent` (appsettings).
- **Default:** 10 (percent).
- When margin % is below this value and profitability is resolved, a **LOW_MARGIN** (Warning) alert is raised.

---

## 5. Data source

- **Single source of truth:** OrderProfitabilityService.
- Alert service **does not** reimplement revenue or payout resolution; it calls the profitability service and derives alerts from the result (status, reason codes, amounts, margin).

---

## 6. Computed live vs persisted

- **Computed only:** GET `/api/orders/{id}/financial-alerts` and POST `/api/orders/financial-alerts/bulk` compute alerts on each request (no persistence).
- **Persisted:** POST `/api/orders/{id}/financial-alerts/evaluate-and-save` evaluates alerts, **replaces** any existing persisted alerts for that order, saves to `OrderFinancialAlerts` table, and calls `IOrderFinancialAlertNotifier` when there are Critical alerts.
- **Entity:** `OrderFinancialAlert` (Id, CompanyId, OrderId, AlertCode, Severity, Message, amounts, IsActive, CreatedAt). One row per alert; an order can have multiple rows.

---

## 7. API

- **GET** `/api/orders/{id}/financial-alerts?referenceDate=optional`  
  Returns `OrderFinancialAlertsResultDto` (Alerts, AlertCount, HighestSeverity) for one order (computed).

- **POST** `/api/orders/financial-alerts/bulk`  
  Body: `{ "orderIds": ["...", "..."], "referenceDate": "optional" }`  
  Returns a list of `OrderFinancialAlertsResultDto` (computed).

- **POST** `/api/orders/{id}/financial-alerts/evaluate-and-save?referenceDate=optional`  
  Evaluates alerts for the order, persists them (replacing existing for that order), notifies when Critical. Returns `OrderFinancialAlertsResultDto`.

- **GET** `/api/financial-alerts?orderId=&severity=&fromUtc=&toUtc=&activeOnly=true`  
  Returns a list of **persisted** alerts (`PersistedOrderFinancialAlertDto`) for the current company. Use for dashboards and history.

**Optional order enrichment (read-only, for badges on list/detail):**

- **GET** `/api/orders/{id}?includeFinancialAlerts=true`  
  Populates from **computed** alerts: `HasFinancialAlert`, `HighestAlertSeverity`, `AlertCount`.

- **GET** `/api/orders/paged?includeFinancialAlerts=true&...`  
  Same (computed) fields on each item in the paged list when company context is available.

- **GET** `/api/orders/{id}?includeFinancialAlertsSummary=true`  
  Populates from **persisted** active alerts only: `HasFinancialAlert`, `HighestAlertSeverity`, `ActiveAlertCount` (and `AlertCount` for backward compatibility). Lightweight; use for alert badges.

- **GET** `/api/orders/paged?includeFinancialAlertsSummary=true&...`  
  Same (persisted summary) fields on each item. Uses a **single batch query** for the page’s order IDs (no per-row queries). Only active alerts count; severity priority: Critical &gt; Warning &gt; Info.

**Enrichment fields:**

| Field | includeFinancialAlerts (computed) | includeFinancialAlertsSummary (persisted) |
|-------|-----------------------------------|------------------------------------------|
| `HasFinancialAlert` | ✓ | ✓ |
| `HighestAlertSeverity` | ✓ | ✓ |
| `AlertCount` | ✓ (computed count) | ✓ (same as ActiveAlertCount when using summary) |
| `ActiveAlertCount` | — | ✓ (active persisted alerts only) |

Default for both flags is **false**. No UI layout or CreateOrderPage changes; enrichment is additive and read-only.

---

## 8. Service location

- **Interface:** `CephasOps.Application.Pnl.Services.IOrderProfitAlertService`
- **Implementation:** `CephasOps.Application.Pnl.Services.OrderProfitAlertService`
- **Options:** `CephasOps.Application.Pnl.ProfitabilityAlertsOptions` (bound from `ProfitabilityAlerts` section)

---

## 9. Notification hook

- **Interface:** `IOrderFinancialAlertNotifier.NotifyCriticalAlertsAsync(orderId, result)`.
- **Default:** `NoOpOrderFinancialAlertNotifier` (no-op). Register a real implementation (e.g. email or in-app) to be called when an order has Critical alerts after evaluate-and-save.

## 10. Future uses

- **Dashboards:** Use GET `/api/financial-alerts` with filters (severity, date range); show orders with Critical/Warning alerts, low-margin lists.
- **Finance review:** Filter by alert code or severity; bulk evaluate for a date range; optionally call evaluate-and-save on order completion.
- **Notifications:** Wire a custom `IOrderFinancialAlertNotifier` for email or in-app Critical alerts.
