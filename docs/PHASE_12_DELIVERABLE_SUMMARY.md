# Phase 12 Deliverable Summary — Billing & Subscription Platform

## Delivered

| Item | Description |
|------|-------------|
| **BillingPlan** | Domain entity; table BillingPlans. Name, Slug, BillingCycle, Price, Currency, IsActive. |
| **TenantSubscription** | Domain entity; table TenantSubscriptions. TenantId, BillingPlanId, Status, StartedAtUtc, CurrentPeriodEndUtc, ExternalSubscriptionId. |
| **TenantUsageRecord** | Domain entity; table TenantUsageRecords. TenantId, MetricKey, Quantity, PeriodStartUtc, PeriodEndUtc (metered usage). |
| **TenantInvoice** | Domain entity; table TenantInvoices. TenantId, TenantSubscriptionId, InvoiceNumber, period, amounts, Status. |
| **IPaymentProvider** | Abstraction: CreateOrUpdateSubscriptionAsync, CancelSubscriptionAsync, ChargeAsync. NoOpPaymentProvider for dev. |
| **IBillingPlanService** | List plans, GetBySlug. |
| **ITenantSubscriptionService** | ListByTenant, GetActive, SubscribeAsync, CancelAsync. |
| **API** | GET /api/billing/plans, GET by slug; GET/POST /api/billing/subscriptions (tenant, me, subscribe, cancel). |
| **Permissions** | AdminBillingPlansView, AdminBillingPlansEdit. |
| **Migration** | Phase12_SubscriptionBilling (BillingPlans, TenantSubscriptions, TenantUsageRecords, TenantInvoices). |

## Verification

- Apply migration (script from Phase11 to Phase12 or `dotnet ef database update`).
- Create a BillingPlan (via seed or future API); call POST .../tenant/{tenantId}/subscribe with { "planSlug": "..." }; GET .../subscriptions/me with tenant context.
